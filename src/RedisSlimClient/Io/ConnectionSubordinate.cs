﻿using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Util;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectionSubordinate : IConnectionSubordinate
    {
        readonly SyncronizedInstance<ICommandPipeline> _pipeline;

        public ConnectionSubordinate(
            ServerEndPointInfo endPointInfo,
            SyncronizedInstance<ICommandPipeline> pipeline)
        {
            _pipeline = pipeline;

            EndPointInfo = endPointInfo;
        }

        public ServerEndPointInfo EndPointInfo { get; private set; }

        public IConnectionSubordinate Clone(ServerEndPointInfo newEndpointInfo)
        {
            return new ConnectionSubordinate(newEndpointInfo, _pipeline);
        }

        public ConnectionMetrics Metrics => _pipeline.TryGet(p => p.Metrics);

        public Task<ICommandPipeline> GetPipeline() => _pipeline.GetValue();

        public PipelineStatus Status => _pipeline.TryGet(p => p.Status, PipelineStatus.Uninitialized);

        public void Dispose()
        {
            _pipeline.Dispose();
        }
    }
}
