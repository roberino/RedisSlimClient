using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Server.Clustering;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Server
{
    class ConnectionInitialiser : IServerNodeInitialiser
    {
        readonly IDictionary<ServerEndPointInfo, IConnectionSubordinate> _connectionCache;
        readonly ServerEndPointInfo _initialEndPoint;
        private readonly NetworkConfiguration _networkConfiguration;
        readonly IClientCredentials _clientCredentials;
        readonly Func<IServerEndpointFactory, Task<ICommandPipeline>> _pipelineFactory;

        public ConnectionInitialiser(
            ServerEndPointInfo endPointInfo, NetworkConfiguration networkConfiguration, IClientCredentials clientCredentials,
            Func<IServerEndpointFactory, Task<ICommandPipeline>> pipelineFactory)
        {
            _initialEndPoint = endPointInfo;
            _networkConfiguration = networkConfiguration;
            _clientCredentials = clientCredentials;
            _pipelineFactory = pipelineFactory;

            _connectionCache = new Dictionary<ServerEndPointInfo, IConnectionSubordinate>();
        }

        public async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync()
        {
            var pipelines = await InitialiseAsync(CreatePipelineConnection(_initialEndPoint));

            _connectionCache.Clear();

            return pipelines;
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

            var roles = await pipeline.ExecuteAdmin(RoleCommand);

            initialPipeline.EndPointInfo.UpdateRole(roles.RoleType);

            if (roles.RoleType == ServerRoleType.Master)
            {
                var info = await pipeline.ExecuteAdmin(InfoCommand);

                if (info.TryGetValue("cluster", out var cluster) && cluster.TryGetValue("cluster_enabled", out var ce) && (long)ce == 1)
                {
                    var clusterNodes = await pipeline.ExecuteAdmin(ClusterCommand);
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
                    var subPipe = await _pipelineFactory(endPointInfo);

                    await Auth(subPipe);

                    subPipe.Initialising.Subscribe(Auth);

                    return subPipe;
                }));
            }

            return connection;
        }

        async Task<ICommandPipeline> Auth(ICommandPipeline pipeline)
        {
            if (_clientCredentials.Password != null)
            {
                if (!await pipeline.ExecuteAdmin(AuthCommand))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.ExecuteAdmin(ClientSetName);

            return pipeline;
        }
    }
}