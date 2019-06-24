using BenchmarkDotNet.Attributes;
using RedisSlimClient.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using RedisSlimClient.Stubs;
using System.Collections.Concurrent;

namespace RedisSlimClient.Benchmarks
{
    [CoreJob]
    [RankColumn, MarkdownExporter]
    public class RedisClientBenchmarks : IDisposable
    {
        const string ServerUri = "tcp://localhost:6379/";

        ConcurrentDictionary<string, IRedisClient> _clients;
        IRedisClient _currentClient;

        [Params(PipelineMode.AsyncPipeline, PipelineMode.Sync)]
        public PipelineMode PipelineMode { get; set; }

        [Params(1, 4)]
        public int ConnectionPoolSize { get; set; }

        [Params(10, 100)]
        public int DataCollectionSize { get; set; }

        [Params(1, 4)]
        public int ParallelOps { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _clients = new ConcurrentDictionary<string, IRedisClient>();
        }

        [IterationSetup]
        public void TestSetup()
        {
            var key = $"{PipelineMode}/{ConnectionPoolSize}";

            _currentClient = _clients.GetOrAdd(key, k =>
                RedisClient.Create(new ClientConfiguration(ServerUri)
                {
                    ConnectionPoolSize = ConnectionPoolSize,
                    PipelineMode = PipelineMode,
                    ConnectTimeout = TimeSpan.FromMilliseconds(500),
                    DefaultTimeout = TimeSpan.FromMilliseconds(500)
                })
            );
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
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach(var x in _clients)
            {
                x.Value.Dispose();
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        async Task SetGetDeleteAsync<T>(T data, string key)
        {
            await _currentClient.SetObjectAsync(key, data);

            await _currentClient.GetObjectAsync<T>(key);

            await _currentClient.DeleteAsync(key);
        }
    }
}