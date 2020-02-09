using RedisTribute.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class CounterTests
    {
        readonly ITestOutputHelper _output;

        public CounterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetCounterAsync_ReadIncrementRead_ReturnsExpectedValues(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var counter = await client.GetCounter(id);

                var val = await counter.ReadAsync();

                Assert.Equal(0, val);

                val = await counter.IncrementAsync();

                Assert.Equal(1, val);

                val = await counter.ReadAsync();

                Assert.Equal(1, val);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetCounterAsync_ExistingKey_ReturnsExpectedValues(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                await client.SetAsync($"counter://" + id, "123");

                var counter = await client.GetCounter(id);

                var val = await counter.ReadAsync();

                Assert.Equal(123, val);

                val = await counter.IncrementAsync();

                Assert.Equal(124, val);
            }
        }
    }
}