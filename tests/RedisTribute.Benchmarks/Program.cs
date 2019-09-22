using BenchmarkDotNet.Running;

namespace RedisTribute.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<RedisClientBenchmarks>();
        }
    }
}
