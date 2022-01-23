using System;
using System.Xml.Linq;

namespace RedisTribute.WebStream
{
    public class CrawlChannel
    {
        public string ChannelName { get; set; } = string.Empty;
    }

    public class CrawlOptions : CrawlChannel
    {
        public Uri? RootUri { get; set; }

        public Func<XDocument, XNode>? ContentSelector { get; set; }

        public TimeSpan NoSubscriberBackoff { get; set; } = TimeSpan.FromSeconds(5);

        public ILinkStrategy? LinkStrategy { get; set; }
    }
}
