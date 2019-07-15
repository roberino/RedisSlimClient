using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Server;
using System;
using System.Linq;

namespace RedisSlimClient.Io
{
    class ConnectionFactory
    {
        public IConnection Create(ClientConfiguration configuration)
        {
            var eps = Enumerable.Range(1, configuration.ConnectionPoolSize).SelectMany(n => configuration.ServerEndpoints).ToArray();

            if (eps.Length == 1)
            {
                return CreateImpl(configuration, eps[0]);
            }

            return new ConnectionPool(eps.Select(e => CreateImpl(configuration, e)).ToArray());
        }

        static IConnection CreateImpl(ClientConfiguration configuration, Uri endPoint)
        {
            var endPointInfo = new ServerEndPointInfo(endPoint.Host, endPoint.Port);
            var initialiser = new ConnectionInitialiser(configuration);

            if (configuration.PipelineMode == PipelineMode.AsyncPipeline || configuration.PipelineMode == PipelineMode.Default)
            {
                return CreatePipelineImpl(configuration, initialiser, endPointInfo);
            }

            return new Connection(endPointInfo, (ep) =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                return SyncCommandPipeline.CreateAsync(socket);
            }, initialiser, configuration.TelemetryWriter);
        }

        static IConnection CreatePipelineImpl(ClientConfiguration configuration, IServerNodeInitialiser initialiser, ServerEndPointInfo endPointInfo)
        {
            return new Connection(endPointInfo, async (ep) =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                await socket.ConnectAsync();
                var socketPipeline = new SocketPipeline(socket, configuration);
                return new AsyncCommandPipeline(socketPipeline, configuration.Scheduler, configuration.TelemetryWriter);
            }, initialiser, configuration.TelemetryWriter);
        }
    }
}