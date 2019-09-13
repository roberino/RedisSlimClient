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

        BrokenPipeline(ServerEndPointInfo endPointInfo, Exception ex)
        {
            _endPointInfo = endPointInfo;
        }

        public static ICommandExecutor Create(ServerEndPointInfo serverEndPoint, Exception ex) => new BrokenPipeline(serverEndPoint, ex);

        public PipelineMetrics Metrics => new PipelineMetrics();

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
        {
            command.AssignedEndpoint = _endPointInfo.EndpointIdentifier;

            throw new ConnectionUnavailableException();
        }
    }
}
