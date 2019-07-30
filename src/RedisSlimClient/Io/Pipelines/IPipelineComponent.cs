using System;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineComponent : IDisposable
    {
        Uri EndpointIdentifier { get; }

        event Action<Exception> Error;

        event Action<PipelineStatus> StateChanged;
    }
}
