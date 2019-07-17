using System;
using System.Threading.Tasks;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Server;

namespace RedisSlimClient.Io
{
    interface IConnectionSubordinate : IDisposable
    {
        PipelineStatus Status { get; }

        ConnectionMetrics Metrics { get; }

        ServerEndPointInfo EndPointInfo { get; }

        Task<ICommandPipeline> GetPipeline();
    }
}