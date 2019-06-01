using RedisSlimClient.Util;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly INetworkStreamFactory _streamFactory;
        readonly AsyncLock<ICommandPipeline> _pipeline;

        public Connection(EndPoint endPoint, Func<INetworkStreamFactory, Task<ICommandPipeline>> pipelineFactory) : this(new SocketStream(endPoint), pipelineFactory)
        {
        }

        public Connection(INetworkStreamFactory streamFactory, Func<INetworkStreamFactory, Task<ICommandPipeline>> pipelineFactory)
        {
            _streamFactory = streamFactory;

            _pipeline = new AsyncLock<ICommandPipeline>(() => pipelineFactory(_streamFactory));

            Id = IdGenerator.Increment().ToString();
        }

        public float WorkLoad => _pipeline.TryGet(p =>
        {
            var pending = p.PendingWork;

            return pending.PendingReads * pending.PendingWrites;
        });

        public string Id { get; }

        public async Task<ICommandPipeline> ConnectAsync()
        {
            return await _pipeline.GetValue();
        }

        public void Dispose()
        {
            _streamFactory.Dispose();
            _pipeline.Dispose();
        }
    }
}
