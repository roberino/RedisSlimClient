using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Pipelines
{
    public static class PipelineExtensions
    {
        public static Task Start(this IPipeline pipeline, CancellationToken cancellation)
        {
            return Task.Run(() => { pipeline.ExecuteAsync(cancellation); },
                cancellation);
        }

        public static PipelineComponent<TRoot, TData> Filter<TRoot, TData>(this PipelineComponent<TRoot, TData> component, Func<TData, bool> predicate)
            where TRoot : IPipeline
        {
            var receiver = new Filter<TRoot, TData>(predicate);
            component.Attach(receiver);
            return receiver;
        }

        public static PipelineComponent<TRoot, TData> HandleError<TRoot, TData>(this PipelineComponent<TRoot, TData> component, Func<TData, Exception, Task> errorHandler)
            where TRoot : IPipeline
        {
            var receiver = new ErrorHandler<TRoot, TData>(errorHandler);
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

        public static IPipeline Sink<TRoot, TIn>(this PipelineComponent<TRoot, TIn> component, Func<TIn, CancellationToken, Task> sink)
            where TRoot : IPipeline
        {
            var sinkComponent = new Sink<TIn>(sink);
            component.Attach(sinkComponent);
            return component.Root;
        }

        public static IPipeline Sink<TRoot, TIn>(this PipelineComponent<TRoot, TIn> component, Action<TIn, CancellationToken> sink)
            where TRoot : IPipeline
        {
            var sinkComponent = new Sink<TIn>((x, c) =>
            {
                sink(x, c);
                return Task.CompletedTask;
            });

            component.Attach(sinkComponent);
            return component.Root;
        }
    }
}