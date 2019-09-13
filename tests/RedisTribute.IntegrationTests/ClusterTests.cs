using System;
using System.Linq;
using RedisTribute.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class ClusterTests
    {
        const int defaultTimeout = 6000;
        readonly ITestOutputHelper _output;

        public ClusterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslClusterSet)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task PingAsync_DifferentPipelineModes_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            using (var client = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task SetAndGetString(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            using (var client = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).CreateClient())
            {
                await client.SetStringAsync("key1", "abc");
                var result = await client.GetStringAsync("key1");
                await client.DeleteAsync("key1");

                Assert.Equal("abc", result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task SetAndMGetString(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            using (var client = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).CreateClient())
            {
                await client.PingAllAsync();

                var keyBase = Guid.NewGuid().ToString().Substring(4);

                foreach (var i in Enumerable.Range(1, 10))
                {
                    var k = $"{keyBase}{i}";

                    await client.SetStringAsync(k, $"val-{i}");
                }

               var mgetResult = (await client.GetStringsAsync(new[] {$"{keyBase}1", $"{keyBase}2", $"{keyBase}3", $"{keyBase}5"})).ToArray();

                Assert.Equal("val-1", mgetResult[0]);
                Assert.Equal("val-2", mgetResult[1]);
                Assert.Equal("val-3", mgetResult[2]);
                Assert.Equal("val-5", mgetResult[3]);
            }
        }
    }
}