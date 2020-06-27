using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.WebStream
{
    public interface IWebCrawler
    {
        event Action<(Uri uri, Exception error)>? Error;
        event Action<HtmlDocument>? Found;
        event Action? Waiting;
        Task Start(CancellationToken cancellation);
    }
}