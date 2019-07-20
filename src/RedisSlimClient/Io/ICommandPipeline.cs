using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        IAsyncEvent<ICommandPipeline> Initialising { get; }

        PipelineStatus Status { get; }

        ConnectionMetrics Metrics { get; }

        Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default);

        Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }
}