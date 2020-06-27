using System;

namespace RedisTribute.WebStream
{
    public class CrawlChannel
    {
        public string? ChannelName { get; set; }
    }

    public class CrawlOptions : CrawlChannel
    {
        public Uri? RootUri { get; set; }

        public ILinkStrategy? LinkStrategy { get; set; }
    }
}
