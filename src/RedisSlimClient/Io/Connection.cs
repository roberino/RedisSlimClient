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
        readonly SyncronizedInstance<IReadOnlyCollection<IConnectedPipeline>> _subConnections;

        public Connection(
            IServerNodeInitialiser serverNodeInitialiser,
            ITelemetryWriter telemetryWriter = null)
        {
            _serverNodeInitialiser = serverNodeInitialiser;
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _subConnections = new SyncronizedInstance<IReadOnlyCollection<IConnectedPipeline>>(_serverNodeInitialiser.InitialiseAsync);

            Id = IdGenerator.Increment().ToString();
        }

        public string Id { get; }

        public ServerEndPointInfo EndPointInfo { get; }

        public async Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            var connections = await GetSubConnections(command);

            return await connections.First().GetPipeline();
        }

        public void Dispose()
        {
            _subConnections.Dispose();
        }

        public async Task<float> CalculateWorkLoad(ICommandIdentity command)
        {
            var connections = await GetSubConnections(command);

            return connections.Min(x => x.Workload);
        }

        Task<IReadOnlyCollection<IConnectedPipeline>> GetSubConnections(ICommandIdentity command)
        {
            return _subConnections.GetValue();
        }
    }
}