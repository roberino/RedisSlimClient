using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.IntegrationTests.Data;
using RedisTribute.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class StringGetTests
    {
        readonly ITestOutputHelper _output;

        public StringGetTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStringAsync_MultipleConcurrentEntries_DataIntegrityMaintained(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            var entries = new ConcurrentDictionary<string, string>();

            const int numberOfItems = 100;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                await Task.WhenAll(Enumerable.Range(1, numberOfItems).Select(async n =>
                {
                    var id = Guid.NewGuid().ToString();
                    var data = ByteGeneration.RandomBytes();

                    entries[id] = data.hash;

                    await client.SetAsync(id, data.data);
                }));

                Assert.Equal(numberOfItems, entries.Count);

                await Task.WhenAll(entries.Select(async kv =>
                {
                    var data = await client.GetAsync(kv.Key);

                    Assert.True(data.Verify(kv.Value), "Invalid entry");
                }));

                await Task.WhenAll(entries.Select(kv => client.DeleteAsync(kv.Key)));
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task ExpireAsync_InSeconds_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                await client.SetAsync(id, "hello");

                var value = await client.ExpireAsync(id, TimeSpan.FromSeconds(1));

                Assert.True(value);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task ExpireAsync_InMilliseconds_RemovedTheKeyAndReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                await client.SetAsync(id, "hello");

                var value = await client.ExpireAsync(id, TimeSpan.FromMilliseconds(5));

                await Task.Delay(5);

                var msg = await client.GetAsync<string>(id);

                Assert.True(value);
                Assert.False(msg.WasFound);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStringAsync_AlternateDb_ReturnsNotFoundResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                await client.SetAsync(id, "hello");

                var value = (string)await client.GetAsync<string>(id);

                Assert.Equal("hello", value);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStringAsync_NotFound_ReturnsNotFoundResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var result = await client.GetAsync<string>(Guid.NewGuid().ToString());

                var found = true;

                var value = (string)result
                    .IfFound(_ => throw new Exception())
                    .IfNotFound(() => { found = false; })
                    .ResolveNotFound(() => "not-found");

                Assert.False(found);
                Assert.Equal("not-found", value);
            }
        }


        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task SetStringAsync_WithCondition_ReturnsOk(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;
            config.FallbackStrategy = FallbackStrategy.None;
            config.DefaultOperationTimeout = TimeSpan.FromMinutes(2);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var data = Enumerable.Range(1, 16).Select(n => (byte)n).ToArray();
                var key = Guid.NewGuid().ToString();

                var result = await client.SetAsync(key, data, SetCondition.SetKeyOnlyIfExists);

                Assert.False(result);

                var result2 = await client.GetAsync<byte[]>(key);

                Assert.False(result2.WasFound);
            }
        }


        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task SetStringAsync_WithExpiry_ReturnsOk(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var data = Enumerable.Range(1, 16).Select(n => (byte)n).ToArray();
                var key = Guid.NewGuid().ToString();

                var result = await client.SetAsync(key, data, (Expiry)DateTime.UtcNow.AddMilliseconds(5));

                Assert.True(result);

                await Task.Delay(10);

                var notFound = false;

                var result2 = await client.GetAsync<byte[]>(key);

                result2.IfNotFound(() => notFound = true);

                Assert.True(notFound);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStringAsync_FoundWithChainedTask_ReturnsResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                await client.SetAsync(id, "hello");

                var value = (string)await client.GetAsync<string>(id).IfFound(async v =>
                {
                    await Task.Delay(1);
                    return v + " there";
                });

                Assert.Equal("hello there", value);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStringAsync_Cancelled_ReturnsTransformedResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            var delay = false;

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                if (delay)
                    Thread.Sleep(1500);
                return r.ForwardResponse();
            }))
            {
                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, new byte[15000]);
                
                using (var cancel = new CancellationTokenSource(15))
                {                    
                    var cancelled = false;
                    delay = true;

                    var result = await client.GetAsync<string>(key, cancel.Token);

                    var value = (string)result
                        .IfFound(v =>
                        {
                            _output.WriteLine(v);
                            throw new Exception("Not cancelled");
                        })
                        .IfCancelled(() => { cancelled = true; })
                        .ResolveCancelled(() => "timeout");

                    Assert.True(cancelled);
                    Assert.Equal("timeout", value);
                }
            }
        }
    }
}