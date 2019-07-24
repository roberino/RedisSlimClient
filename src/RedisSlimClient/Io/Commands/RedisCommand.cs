using RedisSlimClient.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisCommand<T> : IRedisResult<T>
    {
        static readonly Uri UnassignedEndpoint = new Uri("unknown://unassigned:1");

        protected RedisCommand(string commandText, RedisKey key = default) : this(commandText, true, key)
        {
        }

        protected RedisCommand(string commandText, bool requireMaster, RedisKey key)
        {
            RequireMaster = requireMaster;
            CommandText = commandText;
            Key = key;
            CompletionSource = new TaskCompletionSource<T>();
        }

        public Func<object[], Task> OnExecute { get; set; }

        Action<CommandState> _stateChanged;
        Stopwatch _sw;

        void FireStateChange(CommandStatus status)
        {
            if (_stateChanged != null)
            {
                _stateChanged.Invoke(new CommandState(_sw.Elapsed, status, this));
            }
        }

        public Action<CommandState> OnStateChanged
        {
            set
            {
                _stateChanged = value;
                _sw = new Stopwatch();
                _sw.Start();
            }
        }

        public Uri AssignedEndpoint { get; set; } = UnassignedEndpoint;

        private TaskCompletionSource<T> CompletionSource { get; }

        public virtual RedisKey Key { get; }

        public bool RequireMaster { get; }

        public bool CanBeCompleted => !(CompletionSource.Task.IsCanceled || CompletionSource.Task.IsCompleted || CompletionSource.Task.IsFaulted);

        public string CommandText { get; }

        public Task Execute()
        {
            if (OnExecute != null)
            {
                FireStateChange(CommandStatus.Executing);
                return OnExecute(GetArgs());
            }

            return Task.CompletedTask;
        }

        public virtual object[] GetArgs() => Key.IsNull ? new[] { CommandText } : new[] { CommandText, (object)Key.Bytes };

        public virtual void Complete(IRedisObject redisObject)
        {
            try
            {
                if (redisObject is RedisError err)
                {
                    if (CompletionSource.TrySetException(TranslateError(err)))
                        FireStateChange(CommandStatus.Faulted);

                    return;
                }

                if (CompletionSource.TrySetResult(TranslateResult(redisObject)))
                    FireStateChange(CommandStatus.Completed);
            }
            catch (Exception ex)
            {
                if (CompletionSource.TrySetException(ex))
                    FireStateChange(CommandStatus.Faulted);
            }
        }

        protected virtual Exception TranslateError(RedisError err) => new RedisServerException(err.Message);

        protected abstract T TranslateResult(IRedisObject redisObject);

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Cancel();
                return;
            }

            if (CompletionSource.TrySetException(ex))
                FireStateChange(CommandStatus.Abandoned);
        }

        public TaskAwaiter<T> GetAwaiter() => CompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)CompletionSource.Task).GetAwaiter();

        public void Cancel()
        {
            if (CompletionSource.TrySetCanceled())
                FireStateChange(CommandStatus.Cancelled);
        }
    }
}
