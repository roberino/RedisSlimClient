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

        public Task<ICommandPipeline> GetPipeline() => _pipeline.GetValue();

        public float Workload
        {
            get
            {
                var work = _pipeline.TryGet(x => x.PendingWork);

                return work.PendingReads + work.PendingWrites;
            }
        }

        public void Dispose()
        {
            _pipeline.Dispose();
        }
    }
}
