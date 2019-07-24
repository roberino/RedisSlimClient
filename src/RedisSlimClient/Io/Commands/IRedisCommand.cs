using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    enum CommandState
    {
        Uninitialised,
        Executed,
        Abandoned,
        Completed,
        Cancelled,
        Faulted
    }

    interface IRedisResult<T> : IRedisCommand
    {
        new TaskAwaiter<T> GetAwaiter();
    }

    interface IRedisCommand : ICommandIdentity
    {
        Action<ICommandIdentity, CommandState> OnStateChanged { get; set; }
        Func<object[], Task> OnExecute { get; set; }
        bool CanBeCompleted { get; }
        Task Execute();
        void Complete(IRedisObject obj);
        void Cancel();
        void Abandon(Exception ex);
        TaskAwaiter GetAwaiter();
    }
}