using RedisSlimClient.Io.Scheduling;
using System;

namespace RedisSlimClient.Io.Pipelines
{
    interface IDuplexPipeline : ISchedulable, IDisposable
    {
        event Action Faulted;

        IPipelineReceiver Receiver { get; }

        IPipelineSender Sender { get; }
    }
}
