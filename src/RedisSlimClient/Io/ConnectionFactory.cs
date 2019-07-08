using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Net;
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
            var socket = SocketFactory.CreateSocket(configuration, endPoint);

            if (configuration.PipelineMode == PipelineMode.AsyncPipeline || configuration.PipelineMode == PipelineMode.Default)
            {
                return CreatePipelineImpl(configuration, socket);
            }

            return new Connection(() => SyncCommandPipeline.CreateAsync(socket), configuration.TelemetryWriter);
        }

        static IConnection CreatePipelineImpl(ClientConfiguration configuration, IManagedSocket socket)
        {            
            return new Connection(async () =>
            {
                await socket.ConnectAsync();
                var socketPipeline = new SocketPipeline(socket, configuration);
                return new AsyncCommandPipeline(socketPipeline, configuration.Scheduler, configuration.TelemetryWriter);
            }, configuration.TelemetryWriter);
        }
    }
}