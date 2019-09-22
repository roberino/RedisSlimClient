using System;
using System.Net;

namespace RedisTribute.Io.Net
{
    interface IServerEndpointFactory
    {
        Uri EndpointIdentifier { get; }
        EndPoint CreateEndpoint();
    }
}