using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CpuBoundTasks.Benchmarks
{
    [CoreJob]
    [RankColumn, MarkdownExporter]
    public class RedisClientBenchmarks : IDisposable
    {

        [Params(50, 100, 500)]
        public int Iterations { get; set; }

        [Params(4, 8)]
        public int ParallelOperations { get; set; }


        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public void EnqueueWorkAsync()
        {
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}