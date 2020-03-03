using System;
using RedisTribute.Configuration;
using System.Threading.Tasks;
using RedisTribute.Stubs;
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
    }
}
