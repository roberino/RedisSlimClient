using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        PipelineStatus Status { get; }

        ConnectionMetrics Metrics { get; }

        Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }
}