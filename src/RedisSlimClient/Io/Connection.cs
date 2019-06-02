using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly INetworkStreamFactory _streamFactory;
        readonly ITelemetryWriter _telemetryWriter;
        readonly AsyncLock<ICommandPipeline> _pipeline;
        readonly TimeSpan _connectTimeout;

        public Connection(EndPoint endPoint, TimeSpan connectTimeout, ITelemetryWriter telemetryWriter, Func<INetworkStreamFactory, Task<ICommandPipeline>> pipelineFactory) : this(new NetworkStreamFactory(endPoint), telemetryWriter, pipelineFactory)
        {
            _connectTimeout = connectTimeout;
        }

        public Connection(INetworkStreamFactory streamFactory, ITelemetryWriter telemetryWriter, Func<INetworkStreamFactory, Task<ICommandPipeline>> pipelineFactory)
        {
            _streamFactory = streamFactory;
            _telemetryWriter = telemetryWriter;
            _pipeline = new AsyncLock<ICommandPipeline>(() => pipelineFactory(_streamFactory));

            Id = IdGenerator.Increment().ToString();
        }

        public float WorkLoad => _pipeline.TryGet(p =>
        {
            var pending = p.PendingWork;

            return pending.PendingReads * pending.PendingWrites;
        });

        public string Id { get; }

        public Task<ICommandPipeline> ConnectAsync()
        {
            return _telemetryWriter.ExecuteAsync(_ => _pipeline.GetValue(_connectTimeout), nameof(ConnectAsync));
        }

        public void Dispose()
        {
            _streamFactory.Dispose();
            _pipeline.Dispose();
        }
    }
}
