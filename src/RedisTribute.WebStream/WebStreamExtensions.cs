using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types.Messaging;
using RedisTribute.WebStream;

// ReSharper disable once CheckNamespace
namespace RedisTribute
{
    public static class WebStreamExtensions
    {
        public static IWebCrawler CreateWebCrawler(this IPublisherClient client, CrawlOptions crawlOptions)
        {
            var stream = new WebCrawler(new HttpClient(), client, crawlOptions);

            return stream;
        }

        public static Task<ISubscription> ProcessWebDocuments(this ISubscriptionClient client, CrawlChannel channel, Func<HtmlDocument, Task> processor, CancellationToken cancellation = default)
        {
            return client.SubscribeAsync<HtmlDocument>(channel.ChannelName, m => processor(m.Body), cancellation);
        }

        public static IWebStreamer StreamWebDocuments(this ISubscriptionClient subscriberClient, IRedisStreamClient streamClient, CrawlChannel channel)
        {
            return new WebStreamer(streamClient, subscriberClient, channel);
        }
    }
}
