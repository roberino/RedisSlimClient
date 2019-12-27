using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Net;
using RedisTribute.Io.Server;
using RedisTribute.Io.Server.Clustering;
using RedisTribute.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class ConnectionInitialiser : IServerNodeInitialiser
    {
        readonly ConnectionSubordinateFactory _connectionSubordinateFactory;

        readonly ServerEndPointInfo _initialEndPoint;
        readonly NetworkConfiguration _networkConfiguration;
        readonly ITelemetryWriter _telemetryWriter;
        readonly TimeSpan _timeout;

        public ConnectionInitialiser(
            ServerEndPointInfo endPointInfo, NetworkConfiguration networkConfiguration,
            IClientCredentials clientCredentials,
            Func<IServerEndpointFactory, Task<ICommandPipeline>> pipelineFactory,
            ITelemetryWriter telemetryWriter, TimeSpan timeout)
        {
            _initialEndPoint = endPointInfo;
            _networkConfiguration = networkConfiguration;
            _telemetryWriter = telemetryWriter;
            _timeout = timeout;
            _connectionSubordinateFactory = new ConnectionSubordinateFactory(endPointInfo, clientCredentials, pipelineFactory, telemetryWriter, timeout);
        }

        public event Action ConfigurationChanged;

        public async Task<IReadOnlyCollection<IConnectionSubordinate>> CreateNodeSetAsync()
        {
            var cache = new Dictionary<ServerEndPointInfo, IConnectionSubordinate>();
            var pipelines = await InitialiseAsync(cache, _initialEndPoint);

            if (pipelines.Any(p => p.EndPointInfo.IsCluster))
            {
                return pipelines.Select(p => (IConnectionSubordinate)new RedirectingConnection(p, pipelines, OnChangeDetected)).ToArray();
            }

            return pipelines;
        }

        RoleCommand RoleCommand => new RoleCommand(_networkConfiguration).AttachTelemetry(_telemetryWriter);

        InfoCommand InfoCommand => new InfoCommand().AttachTelemetry(_telemetryWriter);

        ClusterNodesCommand ClusterCommand => new ClusterNodesCommand(_networkConfiguration).AttachTelemetry(_telemetryWriter);

        void OnChangeDetected(IRedirectionInfo redirectionInfo)
        {
            if (_telemetryWriter.Enabled)
            {
                var ev = TelemetryEventFactory.Instance.Create($"{nameof(ConnectionInitialiser)}/{nameof(ConfigurationChanged)}");
                ev.Data = redirectionInfo.Location.ToString();
                ev.Severity = Severity.Warn;

                _telemetryWriter.Write(ev);
            }

            ConfigurationChanged?.Invoke();
        }

        async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync(IDictionary<ServerEndPointInfo, IConnectionSubordinate> connectionCache, ServerEndPointInfo endPointInfo, int level = 0)
        {
            if (level > 5)
            {
                throw new InvalidOperationException("Cannot find master");
            }

            var initialPipeline = CreatePipelineConnection(connectionCache, endPointInfo);

            var pipeline = await initialPipeline.GetPipeline();

            var roles = await pipeline.ExecuteAdminWithTimeout(RoleCommand, _timeout);

            initialPipeline.EndPointInfo.UpdateRole(roles.RoleType);

            if (roles.RoleType == ServerRoleType.Master)
            {
                var info = await pipeline.ExecuteAdminWithTimeout(InfoCommand, _timeout);

                if (info.TryGetValue("cluster", out var cluster) && cluster.TryGetValue("cluster_enabled", out var ce) && (long)ce == 1)
                {
                    if (!_connectionSubordinateFactory.IsDefaultDatabase)
                    {
                        throw new InvalidOperationException($"Database invalid when cluster enabled: {_connectionSubordinateFactory.ClientCredentials.Database}");
                    }

                    var clusterNodes = await pipeline.ExecuteAdminWithTimeout(ClusterCommand, _timeout);
                    var me = clusterNodes.FirstOrDefault(n => n.IsMyself);

                    var updatedPipe = initialPipeline;

                    if (me != null)
                    {
                        updatedPipe = initialPipeline.Clone(me);
                    }

                    return new[] { updatedPipe }.Concat(clusterNodes.Where(n => !n.IsMyself).Select(e => CreatePipelineConnection(connectionCache, e))).ToArray();
                }

                return new[] { initialPipeline }.Concat(roles.Slaves.Where(IsPublicSlave).Select(e => CreatePipelineConnection(connectionCache, e))).ToArray();
            }

            if (roles.RoleType == ServerRoleType.Slave)
            {
                return await InitialiseAsync(connectionCache, roles.Master, level + 1);
            }

            throw new NotSupportedException(roles.RoleType.ToString());
        }

        bool IsPublicSlave(ServerEndPointInfo endPointInfo)
        {
            var initialEp = _initialEndPoint.CreateEndpoint();

            if (initialEp is IPEndPoint iipep && HostAddressResolver.IsPrivateNetworkAddress(iipep.Address))
            {
                return true;
            }

            var ep = endPointInfo.CreateEndpoint();

            if (ep is IPEndPoint ipep && HostAddressResolver.IsPrivateNetworkAddress(ipep.Address))
            {
                return false;
            }

            return true;
        }

        IConnectionSubordinate CreatePipelineConnection(IDictionary<ServerEndPointInfo, IConnectionSubordinate> connectionCache, ServerEndPointInfo endPointInfo)
        {
            if (!connectionCache.TryGetValue(endPointInfo, out var connection))
            {
                connectionCache[endPointInfo] = connection = _connectionSubordinateFactory.CreateConnectionSubordinate(endPointInfo);
            }

            return connection;
        }
    }
}