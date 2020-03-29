using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    class Filter<TRoot, TData> : PipelineComponent<TRoot, TData>, IPipelineComponent<TData, TData>
        where TRoot : IPipeline
    {
        readonly Func<TData, bool> _predicate;

        public Filter(Func<TData, bool> predicate)
        {
            _predicate = predicate;
        }

        public async Task ReceiveAsync(TData input, CancellationToken cancellation)
        {
            if (Successors.Count > 0 && _predicate(input))
            {
                await Task.WhenAll(Successors.Select(x => x.ReceiveAsync(input, cancellation)));
            }
        }
    }
}
