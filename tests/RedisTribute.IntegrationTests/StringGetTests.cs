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

            using (var client = config.CreateClient())
            {
                using (var cancel = new CancellationTokenSource(2))
                {
                    var result = await client.GetAsync<string>(Guid.NewGuid().ToString(), cancel.Token);
                    
                    var cancelled = false;

                    var value = (string)result
                        .IfFound(_ => throw new Exception())
                        .IfCancelled(() => { cancelled = true; })
                        .ResolveCancelled(() => "timeout");

                    Assert.True(cancelled);
                    Assert.Equal("timeout", value);
                }
            }
        }
    }
}