using RedisSlimClient.Configuration;
using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    internal static class EndpointUtility
    {
        public static EndPoint CreateEndpoint(this IDnsResolver dnsResolver, Uri uriEndpoint)
        {
            return dnsResolver.CreateEndpoint(uriEndpoint.Host, uriEndpoint.Port);
        }

        public static IPEndPoint CreateEndpoint(this IDnsResolver dnsResolver, string host, int port)
        {
            return new IPEndPoint(dnsResolver.ParseIp(host), port);
        }

        public static IPAddress ParseIp(this IDnsResolver dnsResolver, string host)
        {
            if (dnsResolver == null)
            {
                throw new ArgumentNullException(nameof(dnsResolver));
            }

            if (IPAddress.TryParse(host, out var addr))
            {
                return addr;
            }

            var ips = dnsResolver.Resolve(host);

            return ips.AddressList.First();
        }

        public static bool AreIpEquivalent(this IDnsResolver dnsResolver, string host1, string host2)
        {
            var ip1 = dnsResolver.ParseIp(host1);
            var ip2 = dnsResolver.ParseIp(host2);

            return StructuralComparisons.StructuralEqualityComparer.Equals(ip1.GetAddressBytes(), ip2.GetAddressBytes());
        }
    }
}