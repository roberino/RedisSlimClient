using RedisTribute.Configuration;
using RedisTribute.Types.Pipelines;
using RedisTribute.WebStream;
using System;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Stubs;

namespace RedisTribute.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ClientConfiguration(args.Length > 0 ? args[0] : "localhost:6379");

            Console.Write("option: (s)ubscribe/(c)rawl > ");
            var opt = Console.ReadKey();

            Console.WriteLine();
            Console.WriteLine();

            if (opt.Key == ConsoleKey.S)
            {
                using (var client = config.CreateSubscriberClient())
                using (var streamClient = config.CreateClient())
                {
                    await WebProcessExample(client, streamClient);
                }

                return;
            }
            using (var client = config.CreateClient())
            {
                await WebCrawlExample(client);
            }
        }

        static async Task PerfTest(IRedisClient client)
        {
            var key = Guid.NewGuid().ToString();
            var i = 0;

            await client.SetAsync(key, ObjectGeneration.CreateObjectGraph());

            while (true)
            {
                var result = await client.GetAsync<TestDtoWithGenericCollection<TestComplexDto>>(key);

                Console.WriteLine($"{i++} | Found: {result.WasFound}, {result.AsValue().Id}");
            }
        }

        static async Task WebCrawlExample(IRedisClient client)
        {
            var crawler = client.CreateWebCrawler(new CrawlOptions()
            {
                ChannelName = "stream1",
                RootUri = new Uri("https://en.wikipedia.org/wiki/Main_Page")
            });

            crawler.Found += d =>
            {
                Console.WriteLine($"{d.DocumentUri}:{d.Content?.Root?.Name}");
            };

            crawler.Waiting += () => { Console.WriteLine("Waiting"); };

            await crawler.Start(CancellationToken.None);
        }

        static async Task WebProcessExample(ISubscriptionClient client, IRedisStreamClient streamClient)
        {
            var stream = client.StreamWebDocuments(streamClient, new CrawlChannel {ChannelName = "stream1"});

            await stream.StreamText(t => Console.Write($"{t} "));


            //var processor = await client.ProcessWebDocuments(new CrawlChannel { ChannelName = "stream1" }, d =>
            // {
            //     Console.WriteLine($"Received: {d.DocumentUri}");

            //     return Task.CompletedTask;
            // });

            //await Task.Delay(100000000);

            //await processor.Unsubscribe();
        }

        static async Task StreamExample(IRedisClient client)
        {
            var pipe = client.CreatePipeline<char>(PipelineOptions.FromStartOfStream(Environment.MachineName));

            var cancel = new CancellationTokenSource();

            var receiverTask = pipe.FilterEcho().Sink(Console.Write).Start(cancel.Token);

            while (!cancel.IsCancellationRequested)
            {
                var k = Console.ReadKey();

                if (k.Key == ConsoleKey.Escape)
                {
                    cancel.Cancel();
                    break;
                }

                if (k.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }

                await pipe.PushAsync(k.KeyChar, cancellation: cancel.Token);
            }

            await receiverTask;
        }
    }
}