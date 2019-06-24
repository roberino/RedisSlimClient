﻿using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisCommand : IRedisResult<RedisObject>
    {
        protected RedisCommand(string commandText)
        {
            CommandText = commandText;
            CompletionSource = new TaskCompletionSource<RedisObject>();
        }

        public string CommandText { get; }

        public virtual object[] GetArgs() => new[] { CommandText };

        public virtual void Complete(RedisObject redisObject)
        {
            try
            {
                CompletionSource.SetResult(redisObject);
            }
            catch (Exception ex)
            {
                CompletionSource.SetException(ex);
            }
        }

        public void Abandon(Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                Cancel();
                return;
            }

            CompletionSource.SetException(ex);
        }
        public Func<Task> Execute { get; set; }

        public TaskCompletionSource<RedisObject> CompletionSource { get; }

        public TaskAwaiter<RedisObject> GetAwaiter() => CompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)CompletionSource.Task).GetAwaiter();

        public void Cancel()
        {
            CompletionSource.TrySetCanceled();
        }
    }
}
