using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisCommand<T> : IRedisResult<T>
    {
        protected RedisCommand(string commandText)
        {
            CommandText = commandText;
            CompletionSource = new TaskCompletionSource<T>();
        }

        public bool CanBeCompleted => !(CompletionSource.Task.IsCanceled || CompletionSource.Task.IsCompleted || CompletionSource.Task.IsFaulted);

        public string CommandText { get; }

        public virtual object[] GetArgs() => new[] { CommandText };

        public virtual void Complete(IRedisObject redisObject)
        {
            try
            {
                if (redisObject is RedisError err)
                {
                    CompletionSource.TrySetException(new RedisServerException(err.Message));
                    return;
                }

                CompletionSource.TrySetResult(TranslateResult(redisObject));
            }
            catch (Exception ex)
            {
                CompletionSource.TrySetException(ex);
            }
        }

        protected abstract T TranslateResult(IRedisObject redisObject);

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Cancel();
                return;
            }

            CompletionSource.TrySetException(ex);
        }
        public Func<Task> Execute { get; set; }

        public TaskCompletionSource<T> CompletionSource { get; }

        public TaskAwaiter<T> GetAwaiter() => CompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)CompletionSource.Task).GetAwaiter();

        public void Cancel()
        {
            CompletionSource.TrySetCanceled();
        }
    }
}
