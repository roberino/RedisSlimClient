using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Net;
using System;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    class ServerEndPointInfo : IServerEndpointFactory
    {
        public ServerEndPointInfo(string host, int port, ServerRoleType role = ServerRoleType.Unknown)
        {
            Host = host;
            Port = port;
            RoleType = role;
        }

        public string Host { get; }
        public int Port { get; }

        public ServerRoleType RoleType { get; private set; }

        public void UpdateRole(ServerRoleType role)
        {
            RoleType = role;
        }

        public virtual bool CanServe(ICommandIdentity command) => !command.RequireMaster || RoleType == ServerRoleType.Master;

        public EndPoint CreateEndpoint() => new Uri($"redis://{Host}:{Port}").AsEndpoint();
    }
}