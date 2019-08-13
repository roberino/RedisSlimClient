using System;
using RedisSlimClient.Telemetry;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineComponent : ITraceable, IDisposable
    {
        Uri EndpointIdentifier { get; }

        event Action<Exception> Error;

        event Action<PipelineStatus> StateChanged;
    }
}
