using RedisTribute.Configuration;
using RedisTribute.Types.Geo;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class GeoTests
    {
        readonly ITestOutputHelper _output;

        public GeoTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GeoAdd_TwoMembers_CanRetrieveDistance(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12, 55)));
                await client.GeoAddAsync(key, ("y", (160, 77)));

                var dist = await client.GeoDistAsync(key, "x", "y", DistanceUnit.Metres);

                Assert.True(dist > 0);
            }
        }
    }
}
