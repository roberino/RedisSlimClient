using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Server.Clustering;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Server
{
    class ConnectionInitialiser : IServerNodeInitialiser
    {
        private readonly IClientCredentials _clientCredentials;
        private readonly AuthCommand _authCommand;
        private readonly RoleCommand _roleCommand;
        private readonly ClusterNodesCommand _clusterNodesCommand;
        private readonly ClientSetNameCommand _setNameCommand;

        public ConnectionInitialiser(IClientCredentials clientCredentials)
        {
            _clientCredentials = clientCredentials;

            if (!string.IsNullOrEmpty(_clientCredentials.Password))
            {
                _authCommand = new AuthCommand(_clientCredentials.Password);
            }

            _setNameCommand = new ClientSetNameCommand(clientCredentials.ClientName);
            _roleCommand = new RoleCommand();
            _clusterNodesCommand = new ClusterNodesCommand();
        }

        public async Task<IReadOnlyCollection<ServerEndPointInfo>> InitialiseAsync(ICommandPipeline pipeline)
        {
            if (_authCommand != null)
            {
                if(!await pipeline.Execute(_authCommand))
                {
                    throw new AuthenticationException();
                }
            }

            await pipeline.Execute(_setNameCommand);

            var roles = await pipeline.Execute(_roleCommand);

            return roles;
        }
    }
}