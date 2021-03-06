﻿using RedisTribute.Configuration;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Net;
using RedisTribute.Types;
using System;
using System.Net;

namespace RedisTribute.Io.Server
{
    class ServerEndPointInfo : IServerEndpointFactory, IEquatable<ServerEndPointInfo>, IRedisEndpoint
    {
        Uri _uri;
        int? _dbIndex;

        public ServerEndPointInfo(string host, int port, int mappedPort, IHostAddressResolver dnsResolver, ServerRoleType role = ServerRoleType.Unknown)
        {
            Host = host;
            Port = port;
            MappedPort = mappedPort;
            RoleType = role;
            DnsResolver = dnsResolver;
        }

        public void SetDatabase(int index)
        {
            _dbIndex = index;
            _uri = null;
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

                if (_dbIndex.HasValue)
                {
                    if (uri.Query.Length > 0)
                    {
                        uri.Query += "&";
                    }

                    uri.Query += $"db={_dbIndex}";
                }

                _uri = uri.Uri;

                return uri.Uri;
            }
        }

        public virtual bool IsCluster => false;

        public IHostAddressResolver DnsResolver { get; }

        public string Host { get; }

        public int Port { get; }

        public int MappedPort { get; }

        public ServerRoleType RoleType { get; private set; }

        public void UpdateRole(ServerRoleType role)
        {
            RoleType = role;
            _uri = null;
        }

        public virtual bool CanServe(ICommandIdentity command, RedisKey key = default) => !command.RequireMaster || RoleType == ServerRoleType.Master;

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