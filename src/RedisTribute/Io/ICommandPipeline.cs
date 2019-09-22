using RedisTribute.Io.Commands;
using RedisTribute.Io.Monitoring;
using RedisTribute.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    interface ICommandExecutor
    {
        PipelineStatus Status { get; }

        PipelineMetrics Metrics { get; }

        Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }

    interface ICommandPipeline : ICommandExecutor, IDisposable
    {
        IAsyncEvent<ICommandPipeline> Initialising { get; }

        Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }
}