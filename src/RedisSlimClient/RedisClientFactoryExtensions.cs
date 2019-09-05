using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Net.Proxy;
using RedisSlimClient.Io.Server;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public static class RedisClientFactoryExtensions
    {
        public static IRedisReader CreateReader(this ClientConfiguration configuration)
        {
            return RedisClient.Create(configuration);
        }

        public static IRedisClient CreateClient(this ClientConfiguration configuration)
        {
            return RedisClient.Create(configuration);
        }

        public static async Task<IRedisClient> CreateProxiedClientAsync(this ClientConfiguration configuration, Func<Request, Response> networkInterceptor)
        {
            var i = 0;

            var availablePorts = PortUtility.GetFreePorts(configuration.ServerEndpoints.Length);
            var newEndpoints = configuration.ServerEndpoints.Select(e => new Uri($"{e.Scheme}://{e.Host}:{availablePorts[i++]}")).ToArray();
            var proxyConfig = configuration.Clone(newEndpoints);

            var endpointPairs = configuration.ServerEndpoints.Zip(newEndpoints, (s, p) => (s, p));

            var proxies = endpointPairs
                .Select(e => TcpServer.CreateLocalProxy(configuration.NetworkConfiguration.DnsResolver.CreateEndpoint(e.s), e.p.Port))
                .ToArray();

            foreach (var proxy in proxies)
            {
                var ipSrc = (IPEndPoint)proxy.LocalEndPoint;
                var ipDest = (IPEndPoint)proxy.ForwardingEndPoint;
                proxyConfig.NetworkConfiguration.PortMappings.Map(ipDest.Port, ipSrc.Port);

                if (!ipDest.Address.IsLoopback())
                {
                    proxyConfig.NetworkConfiguration.DnsResolver.Register(HostAddressResolver.LocalHost, ipDest.Address.ToString());
                }

                proxy.Error += e =>
                {
                    Trace.WriteLine(e);
                };

                await proxy.StartAsync(new RequestHandler(networkInterceptor));
            }

            return RedisClient.Create(proxyConfig, () =>
            {
                foreach(var proxy in proxies)
                {
                    proxy.Dispose();
                }
            });
        }
    }
}