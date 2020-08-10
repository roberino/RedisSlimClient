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

        public static Task StreamText(this IWebStreamer webStreamer, Action<string>? onSending = null, CancellationToken cancellation = default)
        {
            var empty = new string[0];

            return webStreamer.Stream(x =>
            {
                string content;

                switch (x.Content)
                {
                    case XText text:
                        content = text.Value;
                        break;
                    case XDocument text:
                        content = text.Root.Value;
                        break;
                    case XElement text:
                        content = text.Value;
                        break;
                    case XComment text:
                        content = text.Value;
                        break;
                    default:
                        return empty;

                }

                // var body = x.Content as XElement; //?.Root?.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "body", StringComparison.OrdinalIgnoreCase));


                return content.Tokenise().Select(t =>
                {
                    onSending?.Invoke(t.Text);

                    return t.Text;
                });
            }, cancellation);
        }
    }
}
