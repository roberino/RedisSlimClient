using RedisTribute.Configuration;
using RedisTribute.Types.Pipelines;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ClientConfiguration(args[0]);

            using (var client = config.CreateClient())
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
}