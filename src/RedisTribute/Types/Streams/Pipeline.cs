using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    public interface IRedisStreamPipeline : IPipeline { }

    interface IStreamSinkFactory
    {
        Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>();
    }

    public abstract class StreamPipeline<TIn> : PipelineComponent<IRedisStreamPipeline, StreamingItem<TIn>>
    {

    }

    class Pipeline<TIn> : StreamPipeline<TIn>, IRedisStreamPipeline, IStreamSinkFactory
    {
        readonly IRedisStreamClient _client;
        readonly IRedisStream<(TIn body, string hash)> _inputStream;
        readonly PipelineOptions _options;

        Pipeline(IRedisStreamClient client, IRedisStream<(TIn body, string hash)> inputStream, PipelineOptions options)
        {
            _client = client;
            _inputStream = inputStream;
            _options = options;

            Root = this;
        }

        public static Pipeline<TIn> Create(IRedisStreamClient client, PipelineOptions options)
        {
            var key = options.ResolvePipelineName<TIn>();
            var stream = client.GetStream<(TIn body, string hash)>(key);

            return new Pipeline<TIn>(client, stream, options);
        }

        public async Task PushAsync(TIn item, string hash = null, CancellationToken cancellation = default)
        {
            var x = (body: item, hash: hash ?? "");

            await _inputStream.WriteAsync(x, cancellation);
        }

        public Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>()
        {
            var key = _options.ResolvePipelineName<TOut>();
            var stream = _client.GetStream<(TOut body, string hash)>(key);
            return new PipelineSink<TOut>(stream);
        }

        public async Task ExecuteAsync(CancellationToken cancellation = default)
        {
            if (Successors.Count == 0)
            {
                return;
            }

            await _inputStream.ReadAsync(async kv =>
            {
                var item = new StreamingItem<TIn>(kv.Key, kv.Value.body, kv.Value.hash);

                await Task.WhenAll(Successors
                    .Select(s => s.ReceiveAsync(item, cancellation)));
            }, _options.StartFrom, exitWhenNoData: _options.ExitWhenNoData, cancellation: cancellation);
        }
    }
}
