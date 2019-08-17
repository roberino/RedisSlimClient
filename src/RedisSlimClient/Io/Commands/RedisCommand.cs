using RedisSlimClient.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    static class EndpointConstants
    {
        public static readonly Uri UnassignedEndpoint = new Uri("unknown://unassigned:1");
    }

    abstract class RedisCommand<T> : IRedisResult<T>
    {
        readonly TaskCompletionSource<T> _completionSource;

        Action<CommandState> _stateChanged;
        Stopwatch _sw;

        protected RedisCommand(string commandText, RedisKey key = default) : this(commandText, true, key)
        {
        }

        protected RedisCommand(string commandText, bool requireMaster, RedisKey key)
        {
            RequireMaster = requireMaster;
            CommandText = commandText;
            Key = key;
            _completionSource = new TaskCompletionSource<T>();
        }

        protected void BeginTimer()
        {
            if (_sw == null)
            {
                _sw = new Stopwatch();
                _sw.Start();
            }
        }

        public Func<object[], Task> OnExecute { get; set; }

        void FireStateChange(CommandStatus status)
        {
            _stateChanged?.Invoke(new CommandState(Elapsed, status, this));
        }

        public Action<CommandState> OnStateChanged
        {
            set
            {
                _stateChanged = value;
                BeginTimer();
            }
        }

        public TimeSpan Elapsed => _sw?.Elapsed ?? TimeSpan.Zero;

        public Uri AssignedEndpoint { get; set; } = EndpointConstants.UnassignedEndpoint;

        public virtual RedisKey Key { get; }

        public bool RequireMaster { get; }

        public bool CanBeCompleted => !(_completionSource.Task.IsCanceled || _completionSource.Task.IsCompleted || _completionSource.Task.IsFaulted);

        public string CommandText { get; }

        public async Task Execute()
        {
            if (OnExecute != null)
            {
                FireStateChange(CommandStatus.Executing);
                await OnExecute(GetArgs());
                FireStateChange(CommandStatus.Executed);
            }
        }

        public virtual object[] GetArgs() => Key.IsNull ? new[] { CommandText } : new[] { CommandText, (object)Key.Bytes };

        public virtual void Complete(IRedisObject redisObject)
        {
            try
            {
                if (redisObject is RedisError err)
                {
                    if (_completionSource.TrySetException(TranslateError(err)))
                        FireStateChange(CommandStatus.Faulted);

                    return;
                }

                if (_completionSource.TrySetResult(TranslateResult(redisObject)))
                    FireStateChange(CommandStatus.Completed);
            }
            catch (Exception ex)
            {
                if (_completionSource.TrySetException(ex))
                    FireStateChange(CommandStatus.Faulted);
            }
        }

        protected virtual Exception TranslateError(RedisError err)
        {
            if (ObjectMovedException.TryParse(err.Message, out var ex))
            {
                return ex;
            }

            return new RedisServerException(err.Message);
        }

        protected abstract T TranslateResult(IRedisObject redisObject);

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Cancel();
                return;
            }

            if (_completionSource.TrySetException(ex))
                FireStateChange(CommandStatus.Abandoned);
        }

        public TaskAwaiter<T> GetAwaiter() => _completionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)_completionSource.Task).GetAwaiter();

        public void Cancel()
        {
            if (_completionSource.TrySetCanceled())
                FireStateChange(CommandStatus.Cancelled);
        }
    }
}
