using RedisTribute.Configuration;
using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Net;
using RedisTribute.Io.Net.Proxy;
using RedisTribute.Io.Server;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Messaging;
using RedisTribute.Util;

namespace RedisTribute
{
    public static class RedisClientFactoryExtensions
    {
        public static IRedisReader CreateReader(this ClientConfiguration configuration)
        {
            return CreateClient(configuration);
        }

        public static IRedisClient CreateClient(this ClientConfiguration configuration)
        {
            DefaultMonitoringStrategy mon = null;

            var client = RedisClient.Create(configuration, () => mon?.Dispose());

            if (configuration.HealthCheckInterval != TimeSpan.Zero)
            {
                mon = new DefaultMonitoringStrategy(client, configuration.TelemetryWriter, configuration.HealthCheckInterval);
            }

            return client;
        }

        public static ISubscriptionClient CreateSubscriberClient(this ClientConfiguration configuration)
        {
            var client = RedisSubscriberClient.Create(configuration);

            return client;
        }

        public static async Task AwaitSubscription(this ISubscription subscription, CancellationToken cancellation = default)
        {
            await cancellation;

            await subscription.Unsubscribe(CancellationToken.None);
        }

        public static async Task<IRedisClient> ConnectAsync(this IRedisClient client, CancellationToken cancellation = default)
        {
            var errors = (await client.PingAllAsync(cancellation)).Where(r => !r.Ok).ToArray();

            if (errors.Length > 0)
            {
                if (errors.Length == 1)
                {
                    throw errors[0].Error;
                }

                throw new AggregateException(errors.Select(e => e.Error));
            }

            return client;
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


            DefaultMonitoringStrategy mon = null;

            var client = RedisClient.Create(proxyConfig, () =>
            {
                mon?.Dispose();
                foreach(var proxy in proxies)
                {
                    proxy.Dispose();
                }
            });

            if (configuration.HealthCheckInterval != TimeSpan.Zero)
            {
                mon = new DefaultMonitoringStrategy(client, configuration.TelemetryWriter, configuration.HealthCheckInterval);
            }

            return client;
        }
    }
}