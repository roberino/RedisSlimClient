using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    public interface IRedisStreamPipeline : IPipeline { }

    interface IStreamSinkFactory
    {
        Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>(string forwardingNamespace = null);
    }

    public abstract class StreamPipeline<TIn> : PipelineComponent<IRedisStreamPipeline, StreamingItem<TIn>>
    {
        public abstract Task PushAsync(TIn item, string correlationId = null, CancellationToken cancellation = default);
    }

    class Pipeline<TIn> : StreamPipeline<TIn>, IRedisStreamPipeline, IStreamSinkFactory
    {
        readonly IRedisStreamClient _client;
        readonly IRedisStream<(TIn body, string hash, string correlationId)> _inputStream;
        readonly PipelineOptions _options;
        readonly IList<IDeletable> _tearDownItems;

        Pipeline(IRedisStreamClient client, IRedisStream<(TIn body, string hash, string correlationId)> inputStream, PipelineOptions options)
        {
            _client = client;
            _inputStream = inputStream;
            _options = options;
            _tearDownItems = new List<IDeletable>();

            Root = this;
        }

        public static Pipeline<TIn> Create(IRedisStreamClient client, PipelineOptions options)
        {
            var key = options.ResolvePipelineName<TIn>();
            var stream = client.GetStream<(TIn body, string hash, string correlationId)>(key);

            return new Pipeline<TIn>(client, stream, options);
        }

        public override async Task PushAsync(TIn item, string correlationId = null, CancellationToken cancellation = default)
        {
            var x = (body: item, hash: string.Empty,
                correlationId: string.IsNullOrEmpty(correlationId) ? Guid.NewGuid().ToString("N") : correlationId);

            await _inputStream.WriteAsync(x, cancellation);
        }

        public async Task DeleteAsync(CancellationToken cancellation = default)
        {
            foreach (var x in _tearDownItems)
            {
                await x.DeleteAsync(cancellation);
            }
        }

        public Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>(string forwardingNamespace = null)
        {
            var key = forwardingNamespace == null
                ? _options.ResolvePipelineName<TOut>()
                : _options.ResolvePipelineName<TOut>(forwardingNamespace);

            var stream = _client.GetStream<(TOut body, string hash, string correlationId)>(key);

            _tearDownItems.Add(stream);

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
                var item = new StreamingItem<TIn>(kv.Key, kv.Value.body, kv.Value.hash).Next();

                await Task.WhenAll(Successors
                    .Select(s => s.ReceiveAsync(item, cancellation)));
            }, _options.StartFrom, exitWhenNoData: _options.ExitWhenNoData, cancellation: cancellation);
        }
    }
}
