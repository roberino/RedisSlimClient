using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Io
{
    class ConnectionFactory
    {
        public IConnection Create(ClientConfiguration configuration)
        {
            var eps = configuration.ServerEndpoints.Select(e => e.AsEndpoint()).ToArray();

            if (configuration.ConnectionPoolSize <= 1)
            {
                return CreateImpl(configuration, eps[0]);
            }

            return new ConnectionPool(Enumerable
                .Range(1, configuration.ConnectionPoolSize).Select(n => CreateImpl(configuration, eps[n == 0 ? 0 : (eps.Length - 1) % (n - 1)])).ToArray());
        }

        static IConnection CreateImpl(ClientConfiguration configuration, EndPoint endPoint)
        {
            if (configuration.PipelineMode == PipelineMode.AsyncPipeline || configuration.PipelineMode == PipelineMode.Default)
            {
                return CreatePipelineImpl(configuration, endPoint);
            }

            var streamFactory = new NetworkStreamFactory(configuration.ServerEndpoints.Single().AsEndpoint(), configuration.ConnectTimeout);

            return new Connection(
                async () => new SyncCommandPipeline(await streamFactory.CreateStreamAsync()),
                    configuration.TelemetryWriter);
        }

        static IConnection CreatePipelineImpl(ClientConfiguration configuration, EndPoint endPoint)
        {
            var socket = new SocketFacade(endPoint, configuration.ConnectTimeout);
            
            return new Connection(async () =>
            {
                await socket.ConnectAsync();
                var socketPipeline = new SocketPipeline(socket, configuration);
                return new AsyncCommandPipeline(socketPipeline, configuration.TelemetryWriter);
            }, configuration.TelemetryWriter);
        }
    }
}