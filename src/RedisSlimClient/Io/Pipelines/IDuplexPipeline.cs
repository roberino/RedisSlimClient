using RedisSlimClient.Io.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IDuplexPipeline : ISchedulable, IDisposable
    {
        event Action Faulted;

        IPipelineReceiver Receiver { get; }

        IPipelineSender Sender { get; }

        Task ResetAsync();
    }
}
