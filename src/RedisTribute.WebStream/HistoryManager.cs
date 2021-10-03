using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.WebStream
{
    public static class CrawlOptionsExtensions
    {
        public static CrawlOptions RestrictHosts(this CrawlOptions crawlOptions, params string[] allowedHosts)
        {
            crawlOptions.LinkStrategy = new HistoryManager(allowedHosts);
            return crawlOptions;
        }
    }

    class HistoryManager : ILinkStrategy
    {
        readonly HashSet<string> _allowedDomains;
        readonly SemaphoreSlim _lock;
        readonly HashSet<Uri> _visitedUris = new HashSet<Uri>();

        public HistoryManager(params string[] allowedHosts)
        {
            _lock = new SemaphoreSlim(1, 1);
            _allowedDomains = new HashSet<string>(allowedHosts, StringComparer.OrdinalIgnoreCase);
        }

        public bool ShouldVisit(Uri uri)
        {
            return (_allowedDomains.Count == 0 || _allowedDomains.Contains(uri.Host)) && !_visitedUris.Contains(uri);
        }

        public async Task<T> Visit<T>(Uri uri, Func<Task<T>> visiting, T defaultValue)
        {
            await _lock.WaitAsync();

            if (!ShouldVisit(uri))
                return defaultValue;

            _visitedUris.Add(uri);

            _lock.Release();

            return await visiting();
        }
    }
}