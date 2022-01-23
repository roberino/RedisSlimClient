using RedisTribute.Configuration;
using RedisTribute.Io.Net;
using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Server;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class ConnectionFactory
    {
        readonly Func<ICommandQueue> _queueFactory;
        readonly PipelineMode? _requiredPipelineMode;

        public ConnectionFactory(Func<ICommandQueue>? queueFactory = null, PipelineMode? requiredPipelineMode = null)
        {
            _queueFactory = queueFactory ?? (() => new CommandQueue());
            _requiredPipelineMode = requiredPipelineMode;
        }

        public ICommandRouter Create(ClientConfiguration configuration)
        {
            var eps = Enumerable.Range(1, configuration.ConnectionPoolSize).SelectMany(n => configuration.ServerEndpoints).ToArray();

            if (eps.Length == 1)
            {
                return CreateSingleRouter(configuration, eps[0]);
            }

            return new ConnectionPool(eps.Select(e => CreateSingleRouter(configuration, e)).ToArray());
        }

        ICommandRouter CreateSingleRouter(ClientConfiguration configuration, Uri endPoint)
        {
            ConnectionInitialiser connectionInit;

            var pipelineMode = _requiredPipelineMode.GetValueOrDefault(configuration.PipelineMode);
            var endPointInfo = new ServerEndPointInfo(endPoint.Host, endPoint.Port, configuration.NetworkConfiguration.PortMappings.Map(endPoint.Port), configuration.NetworkConfiguration.DnsResolver);

            if (pipelineMode == PipelineMode.AsyncPipeline || pipelineMode == PipelineMode.Default)
            {
                connectionInit = new ConnectionInitialiser(endPointInfo, configuration.NetworkConfiguration, configuration, CreateAsyncPipe(configuration), configuration.TelemetryWriter, configuration.ConnectTimeout);
            }
            else
            {
                connectionInit = new ConnectionInitialiser(endPointInfo, configuration.NetworkConfiguration, configuration, CreateSyncPipe(configuration), configuration.TelemetryWriter, configuration.ConnectTimeout);
            }

            return new CommandRouter(connectionInit);
        }

        Func<IServerEndpointFactory, Task<ICommandPipeline>> CreateAsyncPipe(ClientConfiguration configuration)
        {
            return async ep =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                await socket.ConnectAsync();
                var socketPipeline = new SocketPipeline(socket, configuration);

                socket.AttachTelemetry(configuration.TelemetryWriter);
                socketPipeline.AttachTelemetry(configuration.TelemetryWriter);

                return new AsyncCommandPipeline(socketPipeline, socket, configuration.Scheduler, configuration.TelemetryWriter, _queueFactory());
            };
        }

        static Func<IServerEndpointFactory, Task<ICommandPipeline>> CreateSyncPipe(ClientConfiguration configuration)
        {
            return ep =>
            {
                var socket = SocketFactory.CreateSocket(configuration, ep);
                return SyncCommandPipeline.CreateAsync(socket);
            };
        }
    }
}