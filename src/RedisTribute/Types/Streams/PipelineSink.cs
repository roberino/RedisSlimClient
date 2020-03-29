using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    class PipelineSink<TIn> : Sink<StreamingItem<TIn>>
    {
        public PipelineSink(IRedisStream<(TIn body, string hash, string correlationId)> outputStream) : base((x, c) => WriteAsync(outputStream, x, c))
        {
        }

        public static Task WriteAsync(IRedisStream<(TIn body, string hash, string correlationId)> stream, StreamingItem<TIn> item, CancellationToken cancellation)
        {
            return stream.WriteAsync((item.Data, item.Hash, item.CorrelationId), cancellation);
        }
    }
}