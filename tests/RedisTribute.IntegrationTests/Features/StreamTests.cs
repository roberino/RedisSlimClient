using System;
using System.Collections.Generic;
using RedisTribute.Configuration;
using System.Threading.Tasks;
using RedisTribute.Stubs;
using RedisTribute.Types.Streams;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class StreamTests
    {
        readonly ITestOutputHelper _output;

        public StreamTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStream_SomeKey_CanWrite(PipelineMode pipelineMode,
            ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                var stream = await client.GetStream<TestComplexDto>(key);

                var id = await stream.WriteAsync(new TestComplexDto()
                {
                    DataItem1 = "abc",
                    DataItem2 = DateTime.UtcNow
                });

                Assert.True(id.Timestamp.ToDateTime().Year > 2019);

                await stream.DeleteAsync();
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetStream_SomeKey_CanWriteAndRead(PipelineMode pipelineMode,
            ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                var stream = await client.GetStream<TestComplexDto>(key);

                var id1 = await stream.WriteAsync(new TestComplexDto()
                {
                    DataItem1 = "abc",
                    DataItem2 = DateTime.UtcNow
                });

                var id2 = await stream.WriteAsync(new TestComplexDto()
                {
                    DataItem1 = "efg",
                    DataItem2 = DateTime.UtcNow
                });


                var results = new Dictionary<StreamEntryId, TestComplexDto>();

                await stream.ReadAllAsync(x =>
                {
                    results.Add(x.Key, x.Value);

                    return Task.CompletedTask;
                });

                await stream.DeleteAsync();

                Assert.Equal("abc", results[id1].DataItem1);
                Assert.Equal("efg", results[id2].DataItem1);
            }
        }
    }
}
