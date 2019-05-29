using BenchmarkDotNet.Running;

namespace CpuBoundTasks.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<RedisClientBenchmarks>();
        }
    }
}
