using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Server;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class ConnectionFactory
    {
        public ICommandRouter Create(ClientConfiguration configuration)
        {
            var eps = Enumerable.Range(1, configuration.ConnectionPoolSize).SelectMany(n => configuration.ServerEndpoints).ToArray();

            if (eps.Length == 1)
            {
                return CreateImpl(configuration, eps[0]);
            }

            return new ConnectionPool(eps.Select(e => CreateImpl(configuration, e)).ToArray());
        }

        static ICommandRouter CreateImpl(ClientConfiguration configuration, Uri endPoint)
        {
            ConnectionInitialiser connectionInit;

            var endPointInfo = new ServerEndPointInfo(endPoint.Host, endPoint.Port, configuration.NetworkConfiguration.PortMappings.Map(endPoint.Port), configuration.NetworkConfiguration.DnsResolver);

            if (configuration.PipelineMode == PipelineMode.AsyncPipeline || configuration.PipelineMode == PipelineMode.Default)
            {
                connectionInit = new ConnectionInitialiser(endPointInfo, configuration.NetworkConfiguration, configuration, CreateAsyncPipe(configuration), configuration.TelemetryWriter, configuration.ConnectTimeout);
            }
            else
            {
                connectionInit = new ConnectionInitialiser(endPointInfo, configuration.NetworkConfiguration, configuration, CreateSyncPipe(configuration), configuration.TelemetryWriter, configuration.ConnectTimeout);
            }

            return new CommandRouter(connectionInit);
        }

        static Func<IServerEndpointFactory, Task<ICommandPipeline>> CreateSyncPipe(ClientConfiguration configuration)
        {
            return ep =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                return SyncCommandPipeline.CreateAsync(socket);
            };
        }

        static Func<IServerEndpointFactory, Task<ICommandPipeline>> CreateAsyncPipe(ClientConfiguration configuration)
        {
            return async ep =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                await socket.ConnectAsync();
                var socketPipeline = new SocketPipeline(socket, configuration);
                return new AsyncCommandPipeline(socketPipeline, socket, configuration.Scheduler, configuration.TelemetryWriter);
            };
        }
    }
}