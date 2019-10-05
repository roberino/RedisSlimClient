using RedisTribute.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
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

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                Thread.Sleep(5);
                return r.ForwardResponse();
            }))
            {
                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, new byte[15000]);

                using (var cancel = new CancellationTokenSource(1))
                {
                    var result = await client.GetAsync<string>(key, cancel.Token);
                    
                    var cancelled = false;

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