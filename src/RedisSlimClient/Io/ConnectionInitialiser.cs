using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Server.Clustering;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;

namespace RedisSlimClient.Io.Server
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

        public async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync()
        {
            try
            {
                var pipelines = await InitialiseAsync(CreatePipelineConnection(_initialEndPoint));

                return pipelines;
            }
            finally
            {
                _connectionCache.Clear();
            }
        }

        AuthCommand AuthCommand => new AuthCommand(_clientCredentials.Password);

        ClientSetNameCommand ClientSetName => new ClientSetNameCommand(_clientCredentials.ClientName);

        RoleCommand RoleCommand => new RoleCommand(_networkConfiguration);

        InfoCommand InfoCommand => new InfoCommand();

        ClusterNodesCommand ClusterCommand => new ClusterNodesCommand(_networkConfiguration);

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
                _connectionCache[endPointInfo] = connection = new ConnectionSubordinate(endPointInfo, new SyncronizedInstance<ICommandPipeline>(async () =>
                {
                    try
                    {
                        var result = await _telemetryWriter.ExecuteAsync(async ctx =>
                        {
                            ctx.Dimensions[nameof(endPointInfo.Host)] = endPointInfo.Host;
                            ctx.Dimensions[nameof(endPointInfo.Port)] = endPointInfo.Port;
                            ctx.Dimensions[nameof(endPointInfo.MappedPort)] = endPointInfo.MappedPort;

                            var subPipe = await _pipelineFactory(endPointInfo);

                            await Auth(subPipe);

                            ctx.Write(nameof(Auth));

                            subPipe.Initialising.Subscribe(Auth);

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

            return connection;
        }

        async Task<ICommandPipeline> Auth(ICommandPipeline pipeline)
        {
            if (_clientCredentials.Password != null)
            {
                if (!await pipeline.ExecuteAdminWithTimeout(AuthCommand, _timeout))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.ExecuteAdminWithTimeout(ClientSetName, _timeout);

            return pipeline;
        }
    }
}