using System;

namespace RedisSlimClient.Io.Pipelines
{
    interface IDuplexPipeline : IRunnable, IDisposable
    {
        IPipelineReceiver Receiver { get; }

        IPipelineSender Sender { get; }
    }
}
