using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly ITelemetryWriter _telemetryWriter;
        readonly AsyncLock<ICommandPipeline> _pipeline;
        
        public Connection(Func<Task<ICommandPipeline>> pipelineFactory, ITelemetryWriter telemetryWriter = null)
        {
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _pipeline = new AsyncLock<ICommandPipeline>(() => pipelineFactory());

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
            return _telemetryWriter.ExecuteAsync(_ => _pipeline.GetValue(TimeSpan.FromMilliseconds(500)), nameof(ConnectAsync));
        }

        public void Dispose()
        {
            _telemetryWriter.ExecuteAsync(ctx =>
            {
                _pipeline.Dispose();
                return Task.FromResult(1);
            }, nameof(Dispose))
            .GetAwaiter().GetResult();
        }
    }
}