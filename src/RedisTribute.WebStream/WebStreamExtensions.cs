using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LinqInfer.Text;
using RedisTribute.Types.Messaging;
using RedisTribute.WebStream;

// ReSharper disable once CheckNamespace
namespace RedisTribute
{
    public static class WebStreamExtensions
    {
        public static IWebCrawler CreateWebCrawler(this IPublisherClient client, CrawlOptions crawlOptions, Func<HttpClient>? httpClientFactory = null)
        {
            if (httpClientFactory == null)
                httpClientFactory = () => new HttpClient();

            var stream = new WebCrawler(httpClientFactory(), client, crawlOptions);

            return stream;
        }

        public static Task<ISubscription> ProcessWebDocuments(this ISubscriptionClient client, CrawlChannel channel, Func<HtmlDocumentContent, Task> processor, CancellationToken cancellation = default)
        {
            return client.SubscribeAsync<HtmlDocumentContent>(channel.ChannelName, m => processor(m.Body), cancellation);
        }

        public static IWebStreamer StreamWebDocuments(this ISubscriptionClient subscriberClient, IRedisStreamClient streamClient, CrawlChannel channel)
        {
            return new WebStreamer(streamClient, subscriberClient, channel);
        }

        public static Task StreamText(this IWebStreamer webStreamer, Func<HtmlDocumentContent, string?>? textSelector = null, Action<string>? onSending = null, CancellationToken cancellation = default)
        {
            var empty = new string[0];

            textSelector ??= SelectText;

            return webStreamer.Stream(x =>
            {
                var content = textSelector(x);

                if (content == null)
                    return empty;

                return content
                    .Tokenise()
                    .Where(t => t.Type != TokenType.Space).Select(t =>
                {
                    onSending?.Invoke(t.Text);

                    return t.Text;
                });
            }, cancellation);
        }

        static string? SelectText(HtmlDocumentContent x)
        {
            switch (x.Content)
            {
                case XText text:
                    return text.Value;
                case XDocument text:
                    return text.Root.Value;
                case XElement text:
                    return text.Value;
                case XComment text:
                    return text.Value;
                default:
                    return null;
            }
        }
    }
}
