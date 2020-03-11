using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    class ErrorHandler<TRoot, TData> : PipelineComponent<TRoot, TData>, IPipelineComponent<TData, TData>
        where TRoot : IPipeline
    {
        readonly Func<TData, Exception, Task> _handler;

        public ErrorHandler(Func<TData, Exception, Task> handler)
        {
            _handler = handler;
        }

        public async Task ReceiveAsync(TData input, CancellationToken cancellation = default)
        {
            foreach (var successor in Successors)
            {
                if (cancellation.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await successor.ReceiveAsync(input, cancellation);
                }
                catch (Exception ex)
                {
                    await _handler(input, ex);
                }
            }
        }
    }
}
