using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using System.Linq;

namespace RedisSlimClient.Io
{
    class ConnectionFactory
    {
        public IConnection Create(ClientConfiguration configuration)
        {
            if (configuration.ConnectionPoolSize <= 1)
            {
                return CreateImpl(configuration);
            }

            return new ConnectionPool(Enumerable
                .Range(1, configuration.ConnectionPoolSize).Select(n => CreateImpl(configuration)).ToArray());
        }

        static IConnection CreateImpl(ClientConfiguration configuration)
        {
            if (configuration.PipelineMode == PipelineMode.AsyncPipeline)
            {
                return CreatePipelineImpl(configuration);
            }

            var streamFactory = new NetworkStreamFactory(configuration.ServerUri.AsEndpoint(), configuration.ConnectTimeout);

            if (configuration.PipelineMode == PipelineMode.Async)
            {
                return new Connection(
                    async () => new CommandPipeline(await streamFactory.CreateStreamAsync(), configuration.TelemetryWriter),
                    configuration.TelemetryWriter);
            }

            return new Connection(
                async () => new SyncCommandPipeline(await streamFactory.CreateStreamAsync()),
                    configuration.TelemetryWriter);
        }

        static IConnection CreatePipelineImpl(ClientConfiguration configuration)
        {
            var socket = new SocketFacade(configuration.ServerUri.AsEndpoint(), configuration.ConnectTimeout);

            var socketPipeline = new SocketPipeline(socket);

            var pipeline = new AsyncCommandPipeline(socketPipeline, configuration.TelemetryWriter);

            return new Connection(async () =>
            {
                await socket.ConnectAsync();
                return new AsyncCommandPipeline(socketPipeline, configuration.TelemetryWriter);
            }, configuration.TelemetryWriter);
        }
    }
}