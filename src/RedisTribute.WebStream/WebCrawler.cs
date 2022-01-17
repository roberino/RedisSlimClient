﻿using LinqInfer.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.WebStream
{
    class WebCrawler : IWebCrawler
    {
        readonly HttpClient _httpClient;
        readonly IPublisherClient _publisherClient;
        readonly CrawlOptions _options;

        public WebCrawler(HttpClient httpClient, IPublisherClient publisherClient, CrawlOptions options)
        {
            _httpClient = httpClient;
            _publisherClient = publisherClient;
            _options = new CrawlOptions()
            {
                LinkStrategy = options.LinkStrategy ?? new HistoryManager(),
                ChannelName = options.ChannelName ??
                                  throw new ArgumentNullException(nameof(options.ChannelName)),
                RootUri = options.RootUri ?? throw new ArgumentNullException(nameof(options.RootUri))
            };
        }

        public event Action<(Uri uri, Exception error)>? Error;

        public event Action<HtmlDocument>? Found;

        public event Action? Waiting;

        public async Task Start(CancellationToken cancellation)
        {
            await FollowLinks(_options.RootUri!, cancellation);
        }

        async Task FollowLinks(Uri root, CancellationToken cancellation)
        {
            var work = new Queue<Uri>();
            var sw = new Stopwatch();

            work.Enqueue(root);

            while (work.Count > 0)
            {
                var uri = work.Dequeue();

                if (cancellation.IsCancellationRequested)
                    return;

                if (!_options.LinkStrategy!.ShouldVisit(uri))
                    continue;

                var links = await _options.LinkStrategy.Visit(uri, async () =>
                {
                    var doc = await OpenDoc(uri, cancellation);

                    if (doc == null)
                        return Enumerable.Empty<Uri>();

                    int received = 0;

                    while (received == 0)
                    {
                        sw.Restart();

                        received =
                            await _publisherClient.PublishAsync(_options.ChannelName, doc.GetContent(_options.ContentSelector), cancellation: cancellation);

                        sw.Stop();

                        doc.Stats = new RetrievalStats(doc.Stats.RetrieveTime, doc.Stats.ParseTime, sw.Elapsed);

                        if (received > 0)
                        {
                            Found?.Invoke(doc);
                            break;
                        }

                        Waiting?.Invoke();

                        await Task.Delay(_options.NoSubscriberBackoff, cancellation);
                    }

                    return doc.Getlinks();
                }, Enumerable.Empty<Uri>());

                foreach (var link in links)
                {
                    if (_options.LinkStrategy.ShouldVisit(link))
                        work.Enqueue(link);
                }
            }
        }

        async Task<HtmlDocument?> OpenDoc(Uri uri, CancellationToken cancellation)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using var response = await _httpClient.GetAsync(uri, cancellation);

                sw.Stop();

                var retrieveTime = sw.Elapsed;

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();

                    using var textReader = new StreamReader(stream);

                    sw.Restart();
                    
                    var content = textReader.OpenAsHtmlDocument();

                    sw.Stop();

                    var stats = new RetrievalStats(retrieveTime, sw.Elapsed, TimeSpan.Zero);

                    return new HtmlDocument (uri, content, stats);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke((uri, ex));
            }

            return null;
        }
    }
}