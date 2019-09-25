using System;
using System.Linq;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Stubs;
using RedisTribute.Telemetry;

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

            using (var client = await config.CreateClient().ConnectAsync())
            {
                long i = 0;

                while (true)
                {
                    var tasks = Enumerable.Range(1, 10).Select(n => Operation(client, n * i)).ToList();

                    await Task.WhenAll(tasks);

                    i++;
                }
            }
        }

        static async Task Operation(IRedisClient client, long i)
        {
            try
            {
                var obj = ObjectGeneration.CreateObjectGraph();

                var k = i % 100;

                await client.SetAsync($"x{k}", obj);

                var dto = await client.GetAsync<TestDtoWithGenericCollection<TestComplexDto>>($"x{k}");

                await Task.Delay(10);

                Console.WriteLine($"{DateTime.UtcNow.ToString()}: op {i}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(10);
        }
    }

    public class MyDto
    {
        public string Stuff { get; set; }
    }
}