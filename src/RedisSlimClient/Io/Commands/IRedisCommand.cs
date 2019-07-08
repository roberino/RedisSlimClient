using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    interface IRedisResult<T> : IRedisCommand
    {
        new TaskAwaiter<T> GetAwaiter();
    }

    interface IRedisCommand
    {
        bool CanBeCompleted { get; }
        Func<Task> Execute { get; set; }
        string CommandText { get; }
        void Complete(IRedisObject obj);
        void Cancel();
        void Abandon(Exception ex);
        object[] GetArgs();
        TaskAwaiter GetAwaiter();
    }
}