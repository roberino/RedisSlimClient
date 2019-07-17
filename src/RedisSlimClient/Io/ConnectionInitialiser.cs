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
        private readonly IDictionary<ServerEndPointInfo, IConnectionSubordinate> _pipelineCache;
        private readonly ServerEndPointInfo _initialEndPoint;
        private readonly IClientCredentials _clientCredentials;
        private readonly Func<IServerEndpointFactory, Task<ICommandPipeline>> _pipelineFactory;
        private readonly AuthCommand _authCommand;
        private readonly RoleCommand _roleCommand;
        private readonly ClusterNodesCommand _clusterNodesCommand;
        private readonly ClientSetNameCommand _setNameCommand;

        public ConnectionInitialiser(
            ServerEndPointInfo endPointInfo, IClientCredentials clientCredentials,
            Func<IServerEndpointFactory, Task<ICommandPipeline>> pipelineFactory)
        {
            _initialEndPoint = endPointInfo;
            _clientCredentials = clientCredentials;
            _pipelineFactory = pipelineFactory;

            if (!string.IsNullOrEmpty(_clientCredentials.Password))
            {
                _authCommand = new AuthCommand(_clientCredentials.Password);
            }

            _setNameCommand = new ClientSetNameCommand(clientCredentials.ClientName);
            _roleCommand = new RoleCommand();
            _clusterNodesCommand = new ClusterNodesCommand();

            _pipelineCache = new Dictionary<ServerEndPointInfo, IConnectionSubordinate>();
        }

        public async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync()
        {
            var pipelines = await InitialiseAsync(CreatePipeline(_initialEndPoint));

            _pipelineCache.Clear();

            return pipelines;
        }

        async Task<IReadOnlyCollection<IConnectionSubordinate>> InitialiseAsync(IConnectionSubordinate initialPipeline, int level = 0)
        {
            if (level > 5)
            {
                throw new InvalidOperationException();
            }

            var pipeline = await initialPipeline.GetPipeline();

            var roles = await pipeline.Execute(_roleCommand);

            initialPipeline.EndPointInfo.UpdateRole(roles.RoleType);

            if (roles.RoleType == ServerRoleType.Master)
            {
                return new[] { initialPipeline }.Concat(roles.Slaves.Select(CreatePipeline)).ToArray();
            }

            if (roles.RoleType == ServerRoleType.Slave)
            {
                return await InitialiseAsync(CreatePipeline(roles.Master), level + 1);
            }

            throw new NotSupportedException(roles.RoleType.ToString());
        }

        IConnectionSubordinate CreatePipeline(ServerEndPointInfo endPointInfo)
        {
            if (!_pipelineCache.TryGetValue(endPointInfo, out var pipeline))
            {
                _pipelineCache[endPointInfo] = pipeline = new ConnectionSubordinate(endPointInfo, new SyncronizedInstance<ICommandPipeline>(async () =>
                {
                    var subPipe = await _pipelineFactory(endPointInfo);

                    await Auth(subPipe);

                    return subPipe;
                }));
            }

            return pipeline;
        }

        async Task<ICommandPipeline> Auth(ICommandPipeline pipeline)
        {
            if (_authCommand != null)
            {
                if (!await pipeline.Execute(_authCommand))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.Execute(_setNameCommand);

            return pipeline;
        }
    }
}