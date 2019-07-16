using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Util;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectedPipeline : IConnectedPipeline
    {
        readonly SyncronizedInstance<ICommandPipeline> _pipeline;

        public ConnectedPipeline(
            ServerEndPointInfo endPointInfo,
            SyncronizedInstance<ICommandPipeline> pipeline)
        {
            _pipeline = pipeline;

            EndPointInfo = endPointInfo;
        }

        public ServerEndPointInfo EndPointInfo { get; }

        public ConnectionMetrics Metrics => _pipeline.TryGet(p => p.Metrics);

        public Task<ICommandPipeline> GetPipeline() => _pipeline.GetValue();

        public PipelineStatus Status => _pipeline.TryGet(p => p.Status, PipelineStatus.Uninitialized);

        public void Dispose()
        {
            _pipeline.Dispose();
        }
    }
}
