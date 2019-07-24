using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisCommand<T> : IRedisResult<T>
    {
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

        public Action<ICommandIdentity, CommandState> OnStateChanged { get; set; }

        public Uri AssignedEndpoint { get; set; }

        private TaskCompletionSource<T> CompletionSource { get; }

        public virtual RedisKey Key { get; }

        public bool RequireMaster { get; }

        public bool CanBeCompleted => !(CompletionSource.Task.IsCanceled || CompletionSource.Task.IsCompleted || CompletionSource.Task.IsFaulted);

        public string CommandText { get; }

        public Task Execute()
        {
            if (OnExecute != null)
                return OnExecute(GetArgs());

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
                        OnStateChanged?.Invoke(this, CommandState.Faulted);

                    return;
                }

                if (CompletionSource.TrySetResult(TranslateResult(redisObject)))
                    OnStateChanged?.Invoke(this, CommandState.Completed);
            }
            catch (Exception ex)
            {
                if (CompletionSource.TrySetException(ex))
                    OnStateChanged?.Invoke(this, CommandState.Faulted);
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
                OnStateChanged?.Invoke(this, CommandState.Abandoned);
        }

        public TaskAwaiter<T> GetAwaiter() => CompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)CompletionSource.Task).GetAwaiter();

        public void Cancel()
        {
            if (CompletionSource.TrySetCanceled())
                OnStateChanged?.Invoke(this, CommandState.Cancelled);
        }
    }
}
