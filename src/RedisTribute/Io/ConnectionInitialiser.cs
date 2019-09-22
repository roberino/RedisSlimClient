using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Net;
using RedisTribute.Io.Server.Clustering;
using RedisTribute.Telemetry;
using RedisTribute.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace RedisTribute.Io.Server
{
    class ConnectionInitialiser : IServerNodeInitialiser
    {
        readonly IDictionary<ServerEndPointInfo, IConnectionSubordinate> _connectionCache;
        readonly ServerEndPointInfo _initialEndPoint;
        readonly NetworkConfiguration _networkConfiguration;
        readonly IClientCredentials _clientCredentials;
        readonly Func<IServerEndpointFactory, Task<ICommandPipeline>> _pipelineFactory;
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
            _clientCredentials = clientCredentials;
            _pipelineFactory = pipelineFactory;
            _telemetryWriter = telemetryWriter;
            _timeout = timeout;
            _connectionCache = new Dictionary<ServerEndPointInfo, IConnectionSubordinate>();
        }

        public event Action ConfigurationChanged;

        public async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync()
        {
            try
            {
                var pipelines = await InitialiseAsync(CreatePipelineConnection(_initialEndPoint));

                if (pipelines.Any(p => p.EndPointInfo.IsCluster))
                {
                    return pipelines.Select(p => (IConnectionSubordinate)new RedirectingConnection(p, pipelines, OnChangeDetected)).ToArray();
                }

                return pipelines;
            }
            finally
            {
                _connectionCache.Clear();
            }
        }

        AuthCommand AuthCommand(string password) => new AuthCommand(password).AttachTelemetry(_telemetryWriter);

        ClientSetNameCommand ClientSetName => new ClientSetNameCommand(_clientCredentials.ClientName).AttachTelemetry(_telemetryWriter);

        RoleCommand RoleCommand => new RoleCommand(_networkConfiguration).AttachTelemetry(_telemetryWriter);

        InfoCommand InfoCommand => new InfoCommand().AttachTelemetry(_telemetryWriter);

        ClusterNodesCommand ClusterCommand => new ClusterNodesCommand(_networkConfiguration).AttachTelemetry(_telemetryWriter);

        void OnChangeDetected(IRedirectionInfo redirectionInfo)
        {
            if (_telemetryWriter.Enabled)
            {
                _telemetryWriter.Write(new TelemetryEvent()
                {
                    Name = nameof(ConnectionInitialiser),
                    Action = nameof(ConfigurationChanged),
                    Data = redirectionInfo.Location.ToString(),
                    Severity = Severity.Warn
                });
            }

            ConfigurationChanged?.Invoke();
        }

        async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync(IConnectionSubordinate initialPipeline, int level = 0)
        {
            if (level > 5)
            {
                throw new InvalidOperationException();
            }

            var pipeline = await initialPipeline.GetPipeline();

            var roles = await pipeline.ExecuteAdminWithTimeout(RoleCommand, _timeout);

            initialPipeline.EndPointInfo.UpdateRole(roles.RoleType);

            if (roles.RoleType == ServerRoleType.Master)
            {
                var info = await pipeline.ExecuteAdminWithTimeout(InfoCommand, _timeout);

                if (info.TryGetValue("cluster", out var cluster) && cluster.TryGetValue("cluster_enabled", out var ce) && (long)ce == 1)
                {
                    var clusterNodes = await pipeline.ExecuteAdminWithTimeout(ClusterCommand, _timeout);
                    var me = clusterNodes.FirstOrDefault(n => n.IsMyself);

                    var updatedPipe = initialPipeline;

                    if (me != null)
                    {
                        updatedPipe = initialPipeline.Clone(me);
                    }

                    return new[] { updatedPipe }.Concat(clusterNodes.Where(n => !n.IsMyself).Select(CreatePipelineConnection)).ToArray();
                }

                return new[] { initialPipeline }.Concat(roles.Slaves.Select(CreatePipelineConnection)).ToArray();
            }

            if (roles.RoleType == ServerRoleType.Slave)
            {
                return await InitialiseAsync(CreatePipelineConnection(roles.Master), level + 1);
            }

            throw new NotSupportedException(roles.RoleType.ToString());
        }

        IConnectionSubordinate CreatePipelineConnection(ServerEndPointInfo endPointInfo)
        {
            if (!_connectionCache.TryGetValue(endPointInfo, out var connection))
            {
                _connectionCache[endPointInfo] = connection = CreateConnectionSubordinate(endPointInfo);
            }

            return connection;
        }

        ConnectionSubordinate CreateConnectionSubordinate(ServerEndPointInfo endPointInfo)
        {
            return new ConnectionSubordinate(endPointInfo, new SyncronizedInstance<ICommandPipeline>(async () =>
            {
                try
                {
                    var result = await _telemetryWriter.ExecuteAsync(async ctx =>
                    {
                        ctx.Dimensions[nameof(endPointInfo.Host)] = endPointInfo.Host;
                        ctx.Dimensions[nameof(endPointInfo.Port)] = endPointInfo.Port;
                        ctx.Dimensions[nameof(endPointInfo.MappedPort)] = endPointInfo.MappedPort;

                        var subPipe = await _pipelineFactory(endPointInfo);

                        var password = _clientCredentials.PasswordManager.GetPassword(endPointInfo);

                        await Auth(subPipe, password);

                        subPipe.Initialising.Subscribe(p => Auth(p, password));

                        return subPipe;
                    }, nameof(CreatePipelineConnection));

                    return result;
                }
                catch (Exception ex)
                {
                    throw new ConnectionInitialisationException(endPointInfo, ex);
                }
            }));
        }

        async Task<ICommandPipeline> Auth(ICommandPipeline pipeline, string password)
        {
            if (password != null)
            {
                if (!await pipeline.ExecuteAdminWithTimeout(AuthCommand(password), _timeout))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.ExecuteAdminWithTimeout(ClientSetName, _timeout);

            return pipeline;
        }
    }
}