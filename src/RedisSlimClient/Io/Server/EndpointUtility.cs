using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    internal static class EndpointUtility
    {
        public static EndPoint AsEndpoint(this Uri uriEndpoint)
        {
            return ParseEndpoint(uriEndpoint.Host, uriEndpoint.Port);
        }

        public static IPEndPoint ParseEndpoint(string host, int port)
        {
            return new IPEndPoint(ParseIp(host), port);
        }

        public static IPAddress ParseIp(string host)
        {
            if (IPAddress.TryParse(host, out var addr))
            {
                return addr;
            }

            var ips = Dns.GetHostAddresses(host);

            return ips.First();
        }

        public static bool AreIpEquivalent(string host1, string host2)
        {
            var ip1 = ParseIp(host1);
            var ip2 = ParseIp(host2);

            return StructuralComparisons.StructuralEqualityComparer.Equals(ip1.GetAddressBytes(), ip2.GetAddressBytes());
        }
    }
}