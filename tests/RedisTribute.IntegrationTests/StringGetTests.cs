using RedisTribute.Configuration;
using System;
using System.Linq;
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
        public async Task GetStringAsync_NotFound_ReturnsNotFoundResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var x = await client.GetAsync<string>(Guid.NewGuid().ToString());

                var found = true;

                var result = (string)x
                    .IfFound(_ => throw new Exception())
                    .IfNotFound(() => found = false)
                    .IfNotFound(() => "not-found");

                Assert.False(found);
                Assert.Equal("not-found", result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic, 5)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic, 5)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic, 10)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic, 10)]
        public async Task x(PipelineMode pipelineMode, ConfigurationScenario configurationScenario, int iterations)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                foreach (var n in Enumerable.Range(1, iterations))
                {

                }
            }
        }
    }
}