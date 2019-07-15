using System.Net;

namespace RedisSlimClient.Io.Net
{
    interface IServerEndpointFactory
    {
        EndPoint CreateEndpoint();
    }
}