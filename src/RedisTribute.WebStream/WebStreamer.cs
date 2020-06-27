using RedisTribute.Types.Pipelines;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.WebStream
{
    public interface IWebStreamer
    {
        event Action<(Uri uri, Exception error)>? Error;
        event Action<HtmlDocument>? Found;
        Task Stream<T>(Func<HtmlDocument, IEnumerable<T>> transformation, CancellationToken cancellation);
    }

    class WebStreamer : IWebStreamer
    {
        readonly IRedisStreamClient _streamClient;
        readonly ISubscriptionClient _subscriptionClient;
        readonly CrawlChannel _channel;

        public WebStreamer(IRedisStreamClient streamClient, ISubscriptionClient subscriptionClient, CrawlChannel channel)
        {
            _streamClient = streamClient;
            _subscriptionClient = subscriptionClient;
            _channel = channel;
        }

        public event Action<(Uri uri, Exception error)>? Error;

        public event Action<HtmlDocument>? Found;

        public async Task Stream<T>(Func<HtmlDocument, IEnumerable<T>> transformation, CancellationToken cancellation)
        {
            var pipe = _streamClient.CreatePipeline<T>(PipelineOptions.FromStartOfStream(_channel.ChannelName));

            await _subscriptionClient.ProcessWebDocuments(_channel, async x =>
            {
                Found?.Invoke(x);

                try
                {
                    var transformedDoc = transformation(x);

                    foreach (var item in transformedDoc)
                    {
                        await pipe.PushAsync(item, cancellation: cancellation);
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke((x.DocumentUri, ex));
                }
            }, cancellation);
        }
    }
}
