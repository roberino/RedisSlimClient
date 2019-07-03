using System;

namespace RedisSlimClient.Io.Pipelines
{
    interface IDuplexPipeline : IRunnable, IDisposable
    {
        event Action Faulted;

        IPipelineReceiver Receiver { get; }

        IPipelineSender Sender { get; }
    }
}
