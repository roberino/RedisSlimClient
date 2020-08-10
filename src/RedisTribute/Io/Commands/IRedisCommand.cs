using RedisTribute.Types;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RedisTribute.Io.Commands
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
        int AttemptSequence { get; set; }
        Action<CommandState> OnStateChanged { set; }
        Func<object[], ValueTask> OnExecute { set; }
        bool CanBeCompleted { get; }
        Task Execute();
        bool SetResult(IRedisObject obj);
        void Cancel();
        void Abandon(Exception ex);
        TaskAwaiter GetAwaiter();
    }
}