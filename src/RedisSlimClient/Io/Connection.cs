using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly IServerNodeInitialiser _serverNodeInitialiser;
        readonly ITelemetryWriter _telemetryWriter;
        readonly SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>> _subConnections;

        public Connection(
            IServerNodeInitialiser serverNodeInitialiser,
            ITelemetryWriter telemetryWriter = null)
        {
            _serverNodeInitialiser = serverNodeInitialiser;
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _subConnections = new SyncronizedInstance<IReadOnlyCollection<IConnectionSubordinate>>(_serverNodeInitialiser.InitialiseAsync);

            Id = IdGenerator.Increment().ToString();
        }

        public string Id { get; }

        public async Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            var connections = await _subConnections.GetValue();

            var pipe = connections
                .Where(c => (c.Status == PipelineStatus.Ok || c.Status ==  PipelineStatus.Uninitialized) && c.EndPointInfo.CanServe(command))
                .OrderBy(c => c.Metrics.Workload)
                .FirstOrDefault();

            if (pipe == null)
            {
                throw new NoAvailableConnectionException();
            }

            return await pipe.GetPipeline();
        }

        public void Dispose()
        {
            _subConnections.Dispose();
        }
    }
}