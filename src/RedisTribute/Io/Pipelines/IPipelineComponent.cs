using System;
using RedisTribute.Telemetry;

namespace RedisTribute.Io.Pipelines
{
    interface IPipelineComponent : ITraceable, IDisposable
    {
        Uri EndpointIdentifier { get; }

        event Action<Exception> Error;

        event Action<PipelineStatus> StateChanged;
    }
}
