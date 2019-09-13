using RedisTribute.Io.Commands;
using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class BrokenPipeline : ICommandExecutor
    {
        readonly ServerEndPointInfo _endPointInfo;
        private readonly Exception _ex;

        BrokenPipeline(ServerEndPointInfo endPointInfo, Exception ex, PipelineStatus status = PipelineStatus.Broken)
        {
            _endPointInfo = endPointInfo;
            _ex = ex;
            Status = status;
        }

        public static ICommandExecutor CreateBrokenPipeline(ServerEndPointInfo serverEndPoint, Exception ex) => new BrokenPipeline(serverEndPoint, ex);

        public static ICommandExecutor CreateUnavailablePipeline(ServerEndPointInfo serverEndPoint, Exception ex) => new BrokenPipeline(serverEndPoint, ex, PipelineStatus.Disabled);

        public PipelineMetrics Metrics => new PipelineMetrics();

        public PipelineStatus Status { get; }

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
        {
            command.AssignedEndpoint = _endPointInfo.EndpointIdentifier;

            throw new ConnectionUnavailableException(_endPointInfo.EndpointIdentifier, _ex);
        }
    }
}
