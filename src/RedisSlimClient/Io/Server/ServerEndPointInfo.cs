using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Net;
using System;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    class ServerEndPointInfo : IServerEndpointFactory, IEquatable<ServerEndPointInfo>
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

        public EndPoint CreateEndpoint() => EndpointUtility.ParseEndpoint(Host, Port);

        public bool Equals(ServerEndPointInfo other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if(Port != other.Port)
            {
                return false;
            }

            if (string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return EndpointUtility.AreIpEquivalent(Host, other.Host);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerEndPointInfo);
        }

        public override int GetHashCode() => Port;
    }
}