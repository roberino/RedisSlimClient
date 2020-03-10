using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    public interface IRedisStreamPipeline : IPipeline { }

    public abstract class StreamPipeline<TIn> : PipelineComponent<IRedisStreamPipeline, StreamingItem<TIn>>
    {

    }

    class Pipeline<TIn> : StreamPipeline<TIn>, IRedisStreamPipeline
    {
        readonly IRedisStream<TIn> _inputStream;
        readonly PipelineOptions _options;

        public Pipeline(IRedisStream<TIn> inputStream, PipelineOptions options)
        {
            _inputStream = inputStream;
            _options = options;

            Root = this;
        }

        public async Task ExecuteAsync(CancellationToken cancellation = default)
        {
            if (Successors.Count == 0)
            {
                return;
            }

            await _inputStream.ReadAsync(async kv =>
            {
                var item = new StreamingItem<TIn>(kv.Key, kv.Value, "");

                await Task.WhenAll(Successors
                    .Select(s => s.ReceiveAsync(item)));
            }, _options.StartFrom, cancellation: cancellation);
        }
    }
}
