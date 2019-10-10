using RedisTribute.Configuration;
using RedisTribute.Stubs;
using RedisTribute.Telemetry;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ClientConfiguration(args[0])
            {
                TelemetryWriter = new TextTelemetryWriter(Console.WriteLine, Severity.All)
            };

            config.UseApplicationInsights("506daf4c-1db9-46a4-b818-d9036fb198d6");

            ThreadPool.SetMinThreads(32, 32);

            var verbose = config.TelemetryWriter.Severity.HasFlag(Severity.Diagnostic);

            try
            {

                using (var client = await config.CreateClient().ConnectAsync())
                {
                    long i = 0;

                    while (true)
                    {
                        var tasks = Enumerable.Range(0, 10).Select(n => Operation(client, n + i, verbose)).ToList();

                        await Task.WhenAll(tasks);

                        i += 10;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }

        static async Task Operation(IRedisClient client, long i, bool verbose)
        {
            try
            {
                var obj = ObjectGeneration.CreateObjectGraph();

                var k = i % 100;

                await client.SetAsync($"x{k}", obj);

                var dto = await client.GetAsync<TestDtoWithGenericCollection<TestComplexDto>>($"x{k}");

                if (verbose || i % 10000 == 0)
                    Console.WriteLine($"{DateTime.UtcNow.ToString()}: op {i}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class MyDto
    {
        public string Stuff { get; set; }
    }
}