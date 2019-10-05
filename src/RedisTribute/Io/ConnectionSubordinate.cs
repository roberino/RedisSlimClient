using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Server;
using RedisTribute.Util;
using System;
using System.Threading.Tasks;

namespace RedisTribute.Io
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

        public PipelineMetrics Metrics => _pipeline.TryGet(p => p.Metrics);

        public Task<ICommandPipeline> GetPipeline() => _pipeline.GetValue();

        public PipelineStatus Status => _pipeline.TryGet(p => p.Status, PipelineStatus.Uninitialized);

        public void Dispose()
        {
            //if (Status == PipelineStatus.Ok)
            //{
            //    _pipeline
            //        .Execute(pipe => pipe.ExecuteAdminWithTimeout(new QuitCommand(), TimeSpan.FromMilliseconds(100)))
            //        .GetAwaiter()
            //        .GetResult();
            //}

            _pipeline.Dispose();
        }
    }
}