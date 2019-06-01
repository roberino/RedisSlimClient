using BenchmarkDotNet.Attributes;
using RedisSlimClient.Configuration;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Benchmarks
{
    [CoreJob]
    [RankColumn, MarkdownExporter]
    public class RedisClientBenchmarks : IDisposable
    {
        const string ServerUri = "tcp://localhost:6379/";

        IRedisClient _client;

        [Params(true, false)]
        public bool UseAsync { get; set; }

        [Params(1, 4)]
        public int ConnectionPoolSize { get; set; }

        [Params(2, 10, 50)]
        public int DataCollectionSize { get; set; }

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
                UseAsyncronousPipeline = UseAsync
            });
        }

        [Benchmark]
        public void SetAndGetAsync()
        {
            var data = ObjectGeneration.CreateObjectGraph(DataCollectionSize);

            SetGetDeleteAsync(data, data.Id).Wait();
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