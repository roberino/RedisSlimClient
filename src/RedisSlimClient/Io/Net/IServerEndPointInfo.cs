using System;
using System.Net;

namespace RedisSlimClient.Io.Net
{
    interface IServerEndpointFactory
    {
        Uri EndpointIdentifier { get; }
        EndPoint CreateEndpoint();
    }
}