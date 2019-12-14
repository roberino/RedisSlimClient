using RedisTribute.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisTribute.Io.Commands
{
    static class EndpointConstants
    {
        public static readonly Uri UnassignedEndpoint = new Uri("unknown://unassigned:1");
    }

    abstract class RedisCommand<T> : RedisTelemetricCommand, IRedisResult<T>
    {
        readonly TaskCompletionSource<T> _completionSource;

        protected RedisCommand(string commandText, RedisKey key = default) : this(commandText, true, key)
        {
        }

        protected RedisCommand(string commandText, bool requireMaster, RedisKey key) : base(commandText, requireMaster, key)
        {
            _completionSource = new TaskCompletionSource<T>();
        }

        public bool CanBeCompleted => !(_completionSource.Task.IsCanceled || _completionSource.Task.IsCompleted || _completionSource.Task.IsFaulted);

        public async Task Execute()
        {
            if (OnExecute != null)
            {
                FireStateChange(CommandStatus.Executing);
                using (var parameters = GetArgs())
                {
                    await OnExecute(parameters.Values);
                }
                FireStateChange(CommandStatus.Executed);
            }
        }

        public bool SetResult(IRedisObject redisObject)
        {
            try
            {
                if (redisObject is RedisError err)
                {
                    if (_completionSource.TrySetException(TranslateError(err)))
                        FireStateChange(CommandStatus.Faulted);

                    return true;
                }

                if (_completionSource.TrySetResult(TranslateResult(redisObject)))
                    FireStateChange(CommandStatus.Completed);
            }
            catch (Exception ex)
            {
                if (_completionSource.TrySetException(ex))
                    FireStateChange(CommandStatus.Faulted);
            }

            return true;
        }


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

        public void Cancel()
        {
            if (_completionSource.TrySetCanceled())
                FireStateChange(CommandStatus.Cancelled);
        }

        public TaskAwaiter<T> GetAwaiter() => _completionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)_completionSource.Task).GetAwaiter();

        protected virtual CommandParameters GetArgs() => Key.IsNull ? new[] { CommandText } : new[] { CommandText, (object)Key.Bytes };

        protected virtual Exception TranslateError(RedisError err) => err.AsException();

        protected abstract T TranslateResult(IRedisObject redisObject);
    }
}
