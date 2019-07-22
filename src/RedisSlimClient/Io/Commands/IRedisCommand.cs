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

    interface IRedisCommand : ICommandIdentity
    {
        bool CanBeCompleted { get; }
        Func<object[], Task> OnExecute { get; set; }
        Task Execute();
        void Complete(IRedisObject obj);
        void Cancel();
        void Abandon(Exception ex);
        TaskAwaiter GetAwaiter();
    }
}