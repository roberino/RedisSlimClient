using System;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Io.Server
{
    internal static class EndpointParser
    {
        public static EndPoint AsEndpoint(this Uri uriEndpoint)
        {
            return ParseEndpoint(uriEndpoint.Host, uriEndpoint.Port);
        }

        static IPEndPoint ParseEndpoint(string host, int port)
        {
            if (char.IsNumber(host[0]))
            {
                return new IPEndPoint(IPAddress.Parse(host), port);
            }

            var ips = Dns.GetHostAddresses(host);

            return new IPEndPoint(ips.First(), port);
        }
    }
}