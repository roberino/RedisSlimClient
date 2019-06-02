using BenchmarkDotNet.Attributes;
using RedisSlimClient.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using RedisSlimClient.Stubs;

namespace RedisSlimClient.Benchmarks
{
    [CoreJob]
    [RankColumn, MarkdownExporter]
    public class RedisClientBenchmarks : IDisposable
    {
        const string ServerUri = "tcp://localhost:6379/";

        IRedisClient _client;

        [Params(false, true)]
        public bool UseAsync { get; set; }

        [Params(1, 4)]
        public int ConnectionPoolSize { get; set; }

        [Params(10, 100)]
        public int DataCollectionSize { get; set; }

        [Params(1, 4)]
        public int ParallelOps { get; set; }

        [GlobalSetup]
        public void Setup()
        {
        }

        [IterationSetup]
        public void TestSetup()
        {
            _client = RedisClient.Create(new ClientConfiguration(ServerUri)
            {
                ConnectionPoolSize = ConnectionPoolSize,
                UseAsyncronousPipeline = UseAsync,
                ConnectTimeout = TimeSpan.FromMilliseconds(500),
                DefaultTimeout = TimeSpan.FromMilliseconds(500)
            });
        }

        [Benchmark]
        public void SetAndGetAsync()
        {
            var ops = Enumerable.Range(1, ParallelOps)
                .Select(async n =>
                {
                    var data = ObjectGeneration.CreateObjectGraph(DataCollectionSize);

                    await SetGetDeleteAsync(data, data.Id);
                })
                .ToArray();

            Task.WhenAll(ops).Wait();
        }

        [IterationCleanup]
        public void TestCleanup()
        {
            _client.Dispose();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
        }

        public void Dispose()
        {
            Cleanup();
        }

        async Task SetGetDeleteAsync<T>(T data, string key)
        {
            await _client.SetObjectAsync(key, data);

            await _client.GetObjectAsync<T>(key);

            await _client.DeleteAsync(key);
        }
    }
}