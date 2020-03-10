using System;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    public static class PipelineExtensions
    {
        public static PipelineComponent<TRoot, TData> Filter<TRoot, TData>(this PipelineComponent<TRoot, TData> component, Func<TData, bool> predicate)
            where TRoot : IPipeline
        {
            var receiver = new Filter<TRoot, TData>(predicate);
            component.Attach(receiver);
            return receiver;
        }

        public static PipelineComponent<TRoot, TOut> Transform<TRoot, TIn, TOut>(this PipelineComponent<TRoot, TIn> component, Func<TIn, TOut> transformation)
            where TRoot : IPipeline
        {
            var transform = new Transformation<TRoot, TIn, TOut>(transformation);
            component.Attach(transform);
            return transform;
        }

        public static IPipeline Sink<TRoot, TIn>(this PipelineComponent<TRoot, TIn> component, Func<TIn, Task> sink)
            where TRoot : IPipeline
        {
            var sinkComponent = new Sink<TIn>(sink);
            component.Attach(sinkComponent);
            return component.Root;
        }
    }
}