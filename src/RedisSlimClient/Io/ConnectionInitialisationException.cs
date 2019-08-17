﻿using RedisSlimClient.Io.Server;
using System;

namespace RedisSlimClient.Io
{
    public sealed class ConnectionInitialisationException : Exception
    {
        public ConnectionInitialisationException(IRedisEndpoint endpoint, Exception innerException) : base($"{nameof(ConnectionInitialisationException)}-{endpoint.Host}:{endpoint.Port} ({endpoint.RoleType})", innerException)
        {
            ServerEndpoint = endpoint;
        }

        public IRedisEndpoint ServerEndpoint { get; }
    }
}
