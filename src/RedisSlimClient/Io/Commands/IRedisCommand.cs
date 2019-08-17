using RedisSlimClient.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    readonly struct CommandState
    {
        public CommandState(TimeSpan elapsed, CommandStatus status, ICommandIdentity identity)
        {
            Elapsed = elapsed;
            Status = status;
            Identity = identity;
        }

        public TimeSpan Elapsed { get; }
        public CommandStatus Status { get; }
        public ICommandIdentity Identity { get; }
    }

    enum CommandStatus
    {
        Uninitialised,
        Executing,
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
        Action<CommandState> OnStateChanged { set; }
        Func<object[], Task> OnExecute { set; }
        bool CanBeCompleted { get; }
        Task Execute();
        void Complete(IRedisObject obj);
        void Cancel();
        void Abandon(Exception ex);
        TaskAwaiter GetAwaiter();
    }
}