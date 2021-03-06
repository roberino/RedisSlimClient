﻿using System;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Types.Geo;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
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
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12, 55)));
                await client.GeoAddAsync(key, ("y", (160, 77)));

                var dist = await client.GeoDistAsync(key, "x", "y", DistanceUnit.Metres);

                await client.DeleteAsync(key);

                Assert.True(dist > 0);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GeoHash_TwoMembers_CanRetrieveHash(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12, 55)));
                await client.GeoAddAsync(key, ("y", (160, 77)));

                var hashes = await client.GeoHashAsync(key, new[] { "x", "y" });

                await client.DeleteAsync(key);

                Assert.Equal(2, hashes.Count);
                Assert.True(hashes["x"].Length > 0);
                Assert.True(hashes["y"].Length > 0);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GeoPos_TwoMembers_CanRetrieveCoords(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, ("x", (12.943, 55.11)));
                await client.GeoAddAsync(key, ("y", (160.9, 77.666)));

                var coords = await client.GeoPosAsync(key, new[] { "x", "y" });

                var x = await client.DeleteAsync(key);

                Assert.Equal(1, x);
                Assert.Equal(2, coords.Count);
                Assert.Equal(12.943, Math.Round(coords["x"].Longitude, 3));
                Assert.Equal(55.11, Math.Round(coords["x"].Latitude, 2));
                Assert.Equal(160.9, Math.Round(coords["y"].Longitude, 1));
                Assert.Equal(77.666, Math.Round(coords["y"].Latitude, 3));
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task GeoRadius_TwoMembers_ReturnsRadiusInfo(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                await client.GeoAddAsync(key, new[] {
                    ("London", (-0.118092, 51.509865)),
                    ("New York", (-73.93524, 40.730610)) });

                await client.GeoAddAsync(key, ("Mumbai ", (72.877426, 19.076090)));

                var radiusResults1 = await client.GeoRadiusAsync(
                    new GeoRadiusQuery(key, (0, 51), 1000, DistanceUnit.Kilometres, Types.SortOrder.Ascending, GeoRadiusOptions.WithCoord | GeoRadiusOptions.WithHash));

                var radiusResults2 = await client.GeoRadiusAsync(
                    new GeoRadiusQuery(key, (0, 51), 1000, DistanceUnit.Miles, Types.SortOrder.Ascending, GeoRadiusOptions.WithDist));

                Assert.Equal(1, radiusResults1.Count);
                Assert.NotNull(radiusResults1["London"].Hash);
                Assert.Equal(-0.118, Math.Round(radiusResults1["London"].Position.Value.Longitude, 3));
                Assert.Equal(51.51, Math.Round(radiusResults1["London"].Position.Value.Latitude, 3));

                Assert.Null(radiusResults2["London"].Hash);
                Assert.False(radiusResults2["London"].Position.HasValue);
                Assert.True(radiusResults2["London"].Distance > 0);
            }
        }
    }
}
