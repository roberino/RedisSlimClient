using RedisTribute.Io.Scheduling;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    interface IDuplexPipeline : ISchedulable, IDisposable
    {
        event Action Faulted;

        IPipelineReceiver Receiver { get; }

        IPipelineSender Sender { get; }

        Task ResetAsync();
    }
}
