using System;
using RedisTribute.Types.Pipelines;
using RedisTribute.Types.Streams;

namespace RedisTribute
{
    public static class StreamsPipelineExtensions
    {
        public static PipelineComponent<TRoot, StreamingItem<TOut>> Transform<TRoot, TIn, TOut>(this PipelineComponent<TRoot, StreamingItem<TIn>> component, Func<TIn, TOut> transformation)
            where TRoot : IRedisStreamPipeline
        {
            var transform = new Transformation<TRoot, StreamingItem<TIn>, StreamingItem<TOut>>(x => new StreamingItem<TOut>(x.Id, transformation(x.Data), x.Hash, x.ClientId));
            component.Attach(transform);
            return transform;
        }
        public static PipelineComponent<TRoot, StreamingItem<TData>> FilterEcho<TRoot, TData>(this PipelineComponent<TRoot, StreamingItem<TData>> component)
            where TRoot : IRedisStreamPipeline
        {
            var clientName = (component.Root as IRedisStreamPipeline)?.ClientId;

            if (clientName == null)
                throw new ArgumentException();

            return component.Filter(x => x.ClientId != clientName);
        }

        public static IRedisStreamPipeline Sink<TRoot, TIn>(this PipelineComponent<TRoot, StreamingItem<TIn>> component, Action<TIn> sink)
            where TRoot : IRedisStreamPipeline
        {
            return (IRedisStreamPipeline)component.Sink((x, c) => sink(x.Data));
        }

        public static IRedisStreamPipeline ForwardToStream<TRoot, TIn>(this PipelineComponent<TRoot, StreamingItem<TIn>> component, string forwardingNamespace = null)
            where TRoot : IRedisStreamPipeline
        {
            var pipeline = component.Root as IStreamSinkFactory;

            if (pipeline == null)
                throw new ArgumentException();

            var sink = pipeline.CreatePipelineSink<TIn>(forwardingNamespace);

            component.Attach(sink);

            return component.Root;
        }
    }
}