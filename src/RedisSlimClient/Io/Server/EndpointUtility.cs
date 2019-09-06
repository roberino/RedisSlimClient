using RedisSlimClient.Configuration;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace RedisSlimClient.Io.Server
{
    static class EndpointUtility
    {
        public static EndPoint CreateEndpoint(this IHostAddressResolver dnsResolver, Uri uriEndpoint)
        {
            return dnsResolver.CreateEndpoint(uriEndpoint.Host, uriEndpoint.Port);
        }

        public static IPEndPoint CreateEndpoint(this IHostAddressResolver dnsResolver, string host, int port)
        {
            return new IPEndPoint(dnsResolver.ParseIp(host), port);
        }

        public static bool IsLoopback(this IPAddress address)
        {
            if (IPAddress.Loopback.Equals(address))
            {
                return true;
            }

            var addStr = address.ToString();

            return addStr == "::1" || addStr == "127.0.0.1";
        }

        public static IPEndPoint CreateLoopbackEndpoint(int port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        public static IPAddress ParseIp(this IHostAddressResolver dnsResolver, string host)
        {
            if (dnsResolver == null)
            {
                throw new ArgumentNullException(nameof(dnsResolver));
            }

            if (IPAddress.TryParse(host, out var addr))
            {
                return dnsResolver.Resolve(addr);
            }

            var ips = dnsResolver.Resolve(host);

            return ips.AddressList.OrderBy(x => x.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).First();
        }

        public static bool AreIpEquivalent(this IHostAddressResolver dnsResolver, string host1, string host2)
        {
            var ip1 = dnsResolver.ParseIp(host1);
            var ip2 = dnsResolver.ParseIp(host2);

            return StructuralComparisons.StructuralEqualityComparer.Equals(ip1.GetAddressBytes(), ip2.GetAddressBytes());
        }
    }
}