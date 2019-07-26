using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Net;
using System;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    class ServerEndPointInfo : IServerEndpointFactory, IEquatable<ServerEndPointInfo>, IRedisEndpoint
    {
        Uri _uri;

        public ServerEndPointInfo(string host, int port, int mappedPort, IHostAddressResolver dnsResolver, ServerRoleType role = ServerRoleType.Unknown)
        {
            Host = host;
            Port = port;
            MappedPort = mappedPort;
            RoleType = role;
            DnsResolver = dnsResolver;
        }

        public Uri EndpointIdentifier
        {
            get
            {
                var u = _uri;

                if (u != null)
                {
                    return u;
                }

                var endpoint = DnsResolver.CreateEndpoint(Host, MappedPort);

                string resolvedHost;

                try
                {
                    resolvedHost = endpoint.Address.MapToIPv4().ToString();
                }
                catch
                {
                    resolvedHost = Host;
                }

                var uri = new UriBuilder($"{RoleType.ToString()}://{resolvedHost}:{MappedPort}");

                if (Port != MappedPort)
                {
                    uri.Query += $"original-port={Port}";
                }

                if (!string.Equals(resolvedHost, Host))
                {
                    if (uri.Query.Length > 0)
                    {
                        uri.Query += "&";
                    }

                    uri.Query += $"original-host={Host}";
                }

                _uri = uri.Uri;

                return uri.Uri;
            }
        }

        public IHostAddressResolver DnsResolver { get; }

        public string Host { get; }

        public int Port { get; }

        public int MappedPort { get; }

        public ServerRoleType RoleType { get; private set; }

        public void UpdateRole(ServerRoleType role)
        {
            RoleType = role;
        }

        public virtual bool CanServe(ICommandIdentity command) => !command.RequireMaster || RoleType == ServerRoleType.Master;

        public EndPoint CreateEndpoint()
        {
            _uri = null;
            return DnsResolver.CreateEndpoint(Host, MappedPort);
        }

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

            if (Port != other.Port)
            {
                return false;
            }

            if (string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return DnsResolver.AreIpEquivalent(Host, other.Host);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerEndPointInfo);
        }

        public override int GetHashCode() => Port;
    }
}