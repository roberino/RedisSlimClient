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

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GeoHash_TwoMembers_CanRetrieveHash(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12, 55)));
                await client.GeoAddAsync(key, ("y", (160, 77)));

                var hashes = await client.GeoHashAsync(key, new[] { "x", "y" });

                Assert.Equal(2, hashes.Count);
                Assert.True(hashes["x"].Length > 0);
                Assert.True(hashes["y"].Length > 0);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GeoPos_TwoMembers_CanRetrieveCoords(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12.943, 55.11)));
                await client.GeoAddAsync(key, ("y", (160.9, 77.666)));

                var coords = await client.GeoPosAsync(key, new[] { "x", "y" });

                Assert.Equal(2, coords.Count);
                Assert.Equal(12.943, Math.Round(coords["x"].Longitude, 3));
                Assert.Equal(55.11, Math.Round(coords["x"].Latitude, 2));
                Assert.Equal(160.9, Math.Round(coords["y"].Longitude, 1));
                Assert.Equal(77.666, Math.Round(coords["y"].Latitude, 3));
            }
        }
    }
}
