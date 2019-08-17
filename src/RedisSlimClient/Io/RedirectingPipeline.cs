using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class RedirectingConnection : IConnectionSubordinate
    {
        readonly IConnectionSubordinate _innerConnection;
        readonly SyncronizedInstance<ICommandPipeline> _commandExecutor;

        public RedirectingConnection(IConnectionSubordinate innerConnection)
        {
            _innerConnection = innerConnection;
            _commandExecutor = new SyncronizedInstance<ICommandPipeline>(async () =>
            {
                var cmd = await innerConnection.GetPipeline();

                return new RedirectingPipeline(cmd)
                {
                    ConfigurationDetected = r =>
                    {
                        ConfigurationDetected?.Invoke(r);
                    }
                };
            });
        }

        public event Action<IRedirectionInfo> ConfigurationDetected;

        public PipelineStatus Status => _innerConnection.Status;

        public PipelineMetrics Metrics => _innerConnection.Metrics;

        public ServerEndPointInfo EndPointInfo => _innerConnection.EndPointInfo;

        public IConnectionSubordinate Clone(ServerEndPointInfo newEndpointInfo)
        {
            return _innerConnection.Clone(newEndpointInfo);
        }

        public void Dispose()
        {
            _innerConnection.Dispose();
        }

        public Task<ICommandPipeline> GetPipeline() => _commandExecutor.GetValue();

        class RedirectingPipeline : ICommandPipeline
        {
            private readonly ICommandPipeline _innerCommandExecutor;

            public RedirectingPipeline(ICommandPipeline innerCommandExecutor)
            {
                _innerCommandExecutor = innerCommandExecutor;
            }

            public PipelineMetrics Metrics => _innerCommandExecutor.Metrics;
            public IAsyncEvent<ICommandPipeline> Initialising => _innerCommandExecutor.Initialising;
            public PipelineStatus Status => _innerCommandExecutor.Status;
            public Action<IRedirectionInfo> ConfigurationDetected;

            public async Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
            {
                try
                {
                    return await _innerCommandExecutor.Execute(command, cancellation);
                }
                catch (ObjectMovedException ex)
                {
                    ConfigurationDetected?.Invoke(ex);
                    throw;
                }
            }

            public async Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default)
            {
                try
                {
                    return await _innerCommandExecutor.ExecuteAdmin(command, cancellation);
                }
                catch (ObjectMovedException ex)
                {
                    ConfigurationDetected?.Invoke(ex);
                    throw;
                }
            }

            public void Dispose()
            {
                _innerCommandExecutor.Dispose();
            }
        }
    }
}