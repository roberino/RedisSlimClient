using System;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Scheduling;
using RedisTribute.Types;

namespace RedisTribute.Io
{
    interface ICommandQueue : ICommandWorkload, IDisposable
    {
        int QueueSize { get; }
        Task Requeue(Func<Task> synchronisedWork);
        Task Enqueue(IRedisCommand command, CancellationToken cancellation = default);
    }

    interface ICommandWorkload
    {
        Task AbortAll(Exception ex, IWorkScheduler scheduler);
        Func<Task> BindResult(IRedisObject result);
    }
}