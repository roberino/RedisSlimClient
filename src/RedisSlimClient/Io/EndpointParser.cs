using System;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Io
{
    static class EndpointParser
    {
        public static EndPoint AsEndpoint(this Uri uriEndpoint)
        {
            return ParseEndpoint($"{uriEndpoint.Host}:{uriEndpoint.Port}");
        }

        public static IPEndPoint ParseEndpoint(string ipAndPort)
        {
            var parts = ipAndPort.Split(':');
            var port = int.Parse(parts[1]);

            if (char.IsNumber(parts[0][0]))
            {
                return new IPEndPoint(IPAddress.Parse(parts[0]), port);
            }

            var ips = Dns.GetHostAddresses(parts[0]);

            return new IPEndPoint(ips.First(), port);
        }
    }
}