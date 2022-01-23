using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Types.Pipelines;

namespace RedisTribute.Types.Streams
{
    public interface IRedisStreamPipeline : IPipeline { string ClientId { get; } }

    interface IStreamSinkFactory
    {
        Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>(string? forwardingNamespace = null);
    }

    public abstract class StreamPipeline<TIn> : PipelineComponent<IRedisStreamPipeline, StreamingItem<TIn>>
    {
        public abstract Task PushAsync(TIn item, string? correlationId = null, CancellationToken cancellation = default);
    }

    class Pipeline<TIn> : StreamPipeline<TIn>, IRedisStreamPipeline, IStreamSinkFactory
    {
        readonly IRedisStreamClient _client;
        readonly IClientIdentifier _clientId;
        readonly IRedisStream<(TIn body, string hash, string correlationId, string clientId)> _inputStream;
        readonly PipelineOptions _options;
        readonly IList<IDeletable> _tearDownItems;

        Pipeline(IRedisStreamClient client, IClientIdentifier clientId, IRedisStream<(TIn body, string hash, string correlationId, string clientId)> inputStream, PipelineOptions options)
        {
            _client = client;
            _clientId = clientId;
            _inputStream = inputStream;
            _options = options;
            _tearDownItems = new List<IDeletable>();

            Root = this;
        }

        public string ClientId => _clientId.ClientName;

        public static Pipeline<TIn> Create(IRedisStreamClient client, IClientIdentifier clientId, PipelineOptions options)
        {
            var key = options.ResolvePipelineName<TIn>();
            var stream = client.GetStream<(TIn body, string hash, string correlationId, string clientId)>(key);

            return new Pipeline<TIn>(client, clientId, stream, options);
        }

        public override async Task PushAsync(TIn item, string? correlationId = null, CancellationToken cancellation = default)
        {
            var x = (body: item, hash: string.Empty,
                correlationId: string.IsNullOrEmpty(correlationId) ? Guid.NewGuid().ToString("N") : correlationId, clientId: _clientId.ClientName);

            await _inputStream.WriteAsync(x, cancellation);
        }

        public async Task DeleteAsync(CancellationToken cancellation = default)
        {
            foreach (var x in _tearDownItems)
            {
                await x.DeleteAsync(cancellation);
            }
        }

        public Sink<StreamingItem<TOut>> CreatePipelineSink<TOut>(string? forwardingNamespace = null)
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
                var item = new StreamingItem<TIn>(kv.Key, kv.Value.body, kv.Value.hash, kv.Value.clientId).Next();

                await Task.WhenAll(Successors
                    .Select(s => s.ReceiveAsync(item, cancellation)));
            }, _options.StartFrom, exitWhenNoData: _options.ExitWhenNoData, cancellation: cancellation);
        }
    }
}
