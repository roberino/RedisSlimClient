using System;
using System.Threading.Tasks;
using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Server;

namespace RedisTribute.Io
{
    interface IConnectionSubordinate : IDisposable
    {
        PipelineStatus Status { get; }

        PipelineMetrics Metrics { get; }

        ServerEndPointInfo EndPointInfo { get; }

        IConnectionSubordinate Clone(ServerEndPointInfo newEndpointInfo);

        Task<ICommandPipeline> GetPipeline();
    }
}