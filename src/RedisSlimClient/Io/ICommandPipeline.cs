using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    interface ICommandExecutor
    {
        PipelineMetrics Metrics { get; }

        Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }

    interface ICommandPipeline : ICommandExecutor, IDisposable
    {
        IAsyncEvent<ICommandPipeline> Initialising { get; }

        PipelineStatus Status { get; }

        Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }
}