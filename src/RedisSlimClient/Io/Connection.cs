using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IConnection
    {
        static readonly SyncedCounter IdGenerator = new SyncedCounter();

        readonly Func<IServerEndpointFactory, Task<ICommandPipeline>> _pipelineFactory;
        readonly IServerNodeInitialiser _serverNodeInitialiser;
        readonly ITelemetryWriter _telemetryWriter;
        readonly SyncronizedInstance<ICommandPipeline> _pipeline;
        readonly IList<IConnection> _subConnections;

        public Connection(
            ServerEndPointInfo endPointInfo,
            Func<IServerEndpointFactory, Task<ICommandPipeline>> pipelineFactory,
            IServerNodeInitialiser serverNodeInitialiser = null,
            ITelemetryWriter telemetryWriter = null)
        {
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _pipeline = new SyncronizedInstance<ICommandPipeline>(InitPipeline);
            _subConnections = new List<IConnection>();

            Id = IdGenerator.Increment().ToString();
            _pipelineFactory = pipelineFactory;
            _serverNodeInitialiser = serverNodeInitialiser;
            EndPointInfo = endPointInfo;
        }

        public float WorkLoad => _pipeline.TryGet(p =>
        {
            var pending = p.PendingWork;

            return pending.PendingReads * pending.PendingWrites;
        });

        public string Id { get; }

        public ServerEndPointInfo EndPointInfo { get; }

        public Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command)
        {
            return _telemetryWriter.ExecuteAsync(_ => _pipeline.GetValue(), nameof(RouteCommandAsync));
        }

        public void Dispose()
        {
            _telemetryWriter.ExecuteAsync(ctx =>
            {
                _pipeline.Dispose();
                return Task.FromResult(1);
            }, nameof(Dispose))
            .GetAwaiter().GetResult();
        }

        async Task<ICommandPipeline> InitPipeline()
        {
            var pipeline = await _pipelineFactory(EndPointInfo);

            var inf = await _serverNodeInitialiser.InitialiseAsync(pipeline);

            foreach(var item in inf)
            {
                _subConnections.Add(new Connection(item, _pipelineFactory, _serverNodeInitialiser, _telemetryWriter));
            }

            return pipeline;
        }
    }
}