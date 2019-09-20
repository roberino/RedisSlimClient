using BenchmarkDotNet.Attributes;
using RedisTribute.Configuration;
using RedisTribute.Stubs;
using RedisTribute.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Benchmarks
{
    [CoreJob]
    [RankColumn, MarkdownExporter]
    public class RedisClientBenchmarks : IDisposable
    {
        const string ServerUri = "redis://localhost:6379/";

        ConcurrentDictionary<string, IRedisClient> _clients;
        IRedisClient _currentClient;

        [Params(PipelineMode.AsyncPipeline, PipelineMode.Sync)]
        public PipelineMode PipelineMode { get; set; }

        [Params(FallbackStrategy.None, FallbackStrategy.ProactiveRetry, FallbackStrategy.Retry)]
        public FallbackStrategy FallbackStrategy { get; set; }

        [Params(1, 4)]
        public int ConnectionPoolSize { get; set; }

        [Params(5, 10)]
        public int DataCollectionSize { get; set; }

        [Params(true, false)]
        public bool TelemetryOn { get; set; }

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
                new ClientConfiguration(ServerUri)
                {
                    ConnectionPoolSize = ConnectionPoolSize,
                    PipelineMode = PipelineMode,
                    ConnectTimeout = TimeSpan.FromMilliseconds(500),
                    DefaultOperationTimeout = TimeSpan.FromMilliseconds(500),
                    FallbackStrategy = FallbackStrategy,
                    TelemetryWriter = TelemetryOn ? new TextTelemetryWriter(Console.WriteLine, Severity.Error | Severity.Warn | Severity.Info) : NullTelemetry.Instance
                }.CreateClient()
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
            foreach (var x in _clients)
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
            var cancel = new CancellationTokenSource(500);

            try
            {
                await _currentClient.SetAsync(key, data, cancel.Token);

                await _currentClient.GetAsync<T>(key, cancel.Token);

                await _currentClient.DeleteAsync(key, cancel.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                cancel.Dispose();
            }
        }
    }
}