using System;

namespace RedisSlimClient.Io.Pipelines
{
    interface IDuplexPipeline : IDisposable
    {
        IPipelineReceiver Receiver { get; }
    }
}
