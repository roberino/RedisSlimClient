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
        private readonly ServerEndPointInfo _endPointInfo;
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
            _endPointInfo = endPointInfo;
            _clientCredentials = clientCredentials;
            _pipelineFactory = pipelineFactory;

            if (!string.IsNullOrEmpty(_clientCredentials.Password))
            {
                _authCommand = new AuthCommand(_clientCredentials.Password);
            }

            _setNameCommand = new ClientSetNameCommand(clientCredentials.ClientName);
            _roleCommand = new RoleCommand();
            _clusterNodesCommand = new ClusterNodesCommand();
        }

        public async Task<IReadOnlyCollection<IConnectedPipeline>> InitialiseAsync()
        {
            var items = new List<ConnectedPipeline>();

            var pipeline = await _pipelineFactory(_endPointInfo);

            var subItems = await InitialiseAsync(pipeline);

            items.Add(new ConnectedPipeline(_endPointInfo, new SyncronizedInstance<ICommandPipeline>(() => Task.FromResult(pipeline))));
            items.AddRange(await InitialiseAsync(pipeline));

            return items;
        }

        async Task<IEnumerable<ConnectedPipeline>> InitialiseAsync(ICommandPipeline pipeline)
        {
            await Auth(pipeline);

            var roles = await pipeline.Execute(_roleCommand);

            _endPointInfo.UpdateRole(roles.RoleType);

            if (roles.RoleType == ServerRoleType.Master)
            {
                return roles.Slaves.Select(r =>
                {
                    return new ConnectedPipeline(r, new SyncronizedInstance<ICommandPipeline>(async () =>
                    {
                        var subPipe = await _pipelineFactory(r);

                        await Auth(subPipe);

                        return subPipe;
                    }));
                });
            }

            // TODO: Invert - lookup master

            throw new NotSupportedException();
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