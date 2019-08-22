using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class RedirectingConnection : IConnectionSubordinate
    {
        readonly IConnectionSubordinate _innerConnection;
        readonly IReadOnlyCollection<IConnectionSubordinate> _connections;
        readonly SyncronizedInstance<ICommandPipeline> _commandExecutor;

        public RedirectingConnection(IConnectionSubordinate innerConnection, IReadOnlyCollection<IConnectionSubordinate> connections, Action<IRedirectionInfo> configurationChangeHandler)
        {
            _innerConnection = innerConnection;
            _connections = connections;
            _commandExecutor = new SyncronizedInstance<ICommandPipeline>(async () =>
            {
                var cmd = await innerConnection.GetPipeline();

                return new RedirectingPipeline(cmd, Redirect)
                {
                    ConfigurationChangeDetected = r =>
                    {
                        configurationChangeHandler.Invoke(r);
                    }
                };
            });
        }

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
            _commandExecutor.Dispose();
        }

        public Task<ICommandPipeline> GetPipeline() => _commandExecutor.GetValue();

        Task<ICommandPipeline> Redirect(IRedirectionInfo redirectionInfo)
        {
            var match = _connections.FirstOrDefault(c => c.EndPointInfo.DnsResolver.AreIpEquivalent(c.EndPointInfo.Host, redirectionInfo.Location.Host) && c.EndPointInfo.MappedPort == redirectionInfo.Location.Port);

            if (match == null)
            {
                throw new NotSupportedException();
            }

            return match.GetPipeline();

        }

        class RedirectingPipeline : ICommandPipeline
        {
            readonly ICommandPipeline _innerCommandExecutor;
            readonly Func<IRedirectionInfo, Task<ICommandPipeline>> _redirectionResolver;

            public RedirectingPipeline(ICommandPipeline innerCommandExecutor, Func<IRedirectionInfo, Task<ICommandPipeline>> redirectionResolver)
            {
                _innerCommandExecutor = innerCommandExecutor;
                _redirectionResolver = redirectionResolver;
            }

            public PipelineMetrics Metrics => _innerCommandExecutor.Metrics;
            public IAsyncEvent<ICommandPipeline> Initialising => _innerCommandExecutor.Initialising;
            public PipelineStatus Status => _innerCommandExecutor.Status;
            public Action<IRedirectionInfo> ConfigurationChangeDetected;

            public async Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
            {
                try
                {
                    return await _innerCommandExecutor.Execute(command, cancellation);
                }
                catch (ObjectMovedException ex)
                {
                    var newLocation = await _redirectionResolver(ex);

                    ConfigurationChangeDetected?.Invoke(ex);

                    return await newLocation.ExecuteAdmin(command, cancellation);
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
                    var newLocation = await _redirectionResolver(ex);

                    ConfigurationChangeDetected?.Invoke(ex);

                    return await newLocation.ExecuteAdmin(command, cancellation);
                }
            }

            public void Dispose()
            {
                ConfigurationChangeDetected = null;
                _innerCommandExecutor.Dispose();
            }
        }
    }
}