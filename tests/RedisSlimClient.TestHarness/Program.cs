using System;
using System.Threading.Tasks;
using RedisSlimClient.Configuration;
using RedisSlimClient.Telemetry;

namespace RedisSlimClient.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ClientConfiguration(args[0])
            {
                TelemetryWriter = new TextTelemetryWriter(Console.WriteLine, Severity.All)
            };

            using (var client = config.CreateClient())
            {
                while (true)
                {
                    try
                    {
                        var result = await client.PingAllAsync();

                        foreach (var response in result)
                        {
                            Console.WriteLine($"PING {response.Endpoint}{response.Ok}: {response.Error}");
                        }

                        await client.SetStringAsync("x2", Guid.NewGuid().ToString());

                        //await client.SetObjectAsync("x1", new MyDto()
                        //{
                        //    Stuff = Guid.NewGuid().ToString()
                        //});
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    await Task.Delay(10);
                }
            }
        }
    }

    public class MyDto
    {
        public string Stuff { get; set; }
    }
}