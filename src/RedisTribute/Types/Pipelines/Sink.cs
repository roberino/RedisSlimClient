using System;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    class Sink<TIn> : IPipelineReceiver<TIn>
    {
        readonly Func<TIn, Task> _sink;

        public Sink(Func<TIn, Task> sink)
        {
            _sink = sink;
        }

        public Task ReceiveAsync(TIn input)
        {
            return _sink(input);
        }
    }
}