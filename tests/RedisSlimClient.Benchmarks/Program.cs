using BenchmarkDotNet.Running;

namespace RedisSlimClient.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<RedisClientBenchmarks>();
        }
    }
}
