using RedisTribute.Configuration;
using RedisTribute.Stubs;
using Serilog;
using Serilog.Sinks.Elasticsearch;
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
            var config = new ClientConfiguration(args[0]);

            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
                {
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
                });

            config.UseApplicationInsights("506daf4c-1db9-46a4-b818-d9036fb198d6");
            config.UseSerilog(loggerConfig);

            ThreadPool.SetMinThreads(32, 32);

            try
            {

                using (var client = await config.CreateClient().ConnectAsync())
                {
                    long i = 0;

                    while (true)
                    {
                        var tasks = Enumerable.Range(0, 10).Select(n => Operation(client, n + i, false)).ToList();

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