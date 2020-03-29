using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    class Sink<TIn> : IPipelineReceiver<TIn>
    {
        readonly Func<TIn, CancellationToken, Task> _sink;

        public Sink(Func<TIn, CancellationToken, Task> sink)
        {
            _sink = sink;
        }

        public Task ReceiveAsync(TIn input, CancellationToken cancellation)
        {
            if (cancellation.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            return _sink(input, cancellation);
        }
    }
}