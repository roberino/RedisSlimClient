using System;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Stubs;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class HashTests
    {
        readonly ITestOutputHelper _output;

        public HashTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task SetHashFieldAsync_SomeFieldAndValue_ReturnsOk(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var response = await client.SetHashFieldAsync(id, "field-a", new byte[] { 1, 2, 3 });

                Assert.True(response);

                var value = await client.GetAsync("");
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task GetHashSetAsync_OfStringValues_CanAddValuesAndSave(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var lookup = await client.GetHashSetAsync<string>(id);

                lookup["x"] = "abc";
                lookup["x=>y"] = "defgh";

                await lookup.SaveAsync();

                var lookup2 = await client.GetHashSetAsync<string>(id);

                Assert.Equal("abc", lookup["x"]);
                Assert.Equal("defgh", lookup["x=>y"]);

                lookup2["z"] = "12345";

                await lookup2.SaveAsync();

                await lookup.RefreshAsync();

                Assert.Equal("12345", lookup["z"]);

                await lookup.DeleteAsync();
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetHashSetAsync_OfCustomType_CanAddValuesAndSave(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var lookup = await client.GetHashSetAsync<TestComplexDto>(id);

                lookup["x"] = new TestComplexDto() { DataItem1 = "abc" };
                lookup["y"] = new TestComplexDto() { DataItem1 = "xyz" };

                await lookup.SaveAsync();

                await lookup.DeleteAsync();
            }
        }
    }
}