using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    class Transformation<TRoot, TIn, TOut> : PipelineComponent<TRoot, TOut>, IPipelineComponent<TIn, TOut>
        where TRoot : IPipeline
    {
        readonly Func<TIn, TOut> _transformation;

        public Transformation(Func<TIn, TOut> transformation)
        {
            _transformation = transformation;
        }

        public async Task ReceiveAsync(TIn input, CancellationToken cancellation)
        {
            if (Successors.Count > 0)
            {
                var tx = _transformation(input);

                await Task.WhenAll(Successors.Select(x => x.ReceiveAsync(tx, cancellation)));
            }
        }
    }
}