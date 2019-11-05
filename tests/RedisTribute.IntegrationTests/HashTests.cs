using RedisTribute.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
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
        public async Task SetHashField_SomeFieldAndValue_ReturnsOk(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var response = await client.SetHashField(id, "field-a", new byte[] { 1, 2, 3 });

                Assert.True(response);

                var value = await client.GetAsync("");
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetHashSet_CanAddValuesAndSave(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var id = Guid.NewGuid().ToString();

                var lookup = await client.GetHashSet<string>(id);

                lookup["x"] = "abc";
                lookup["x=>y"] = "defgh";

                await lookup.SaveAsync();

                var lookup2 = await client.GetHashSet<string>(id);

                Assert.Equal("abc", lookup["x"]);
                Assert.Equal("defgh", lookup["x=>y"]);

                lookup2["z"] = "12345";

                await lookup2.SaveAsync();

                await lookup.RefreshAsync();

                Assert.Equal("12345", lookup["z"]);
            }
        }
    }
}