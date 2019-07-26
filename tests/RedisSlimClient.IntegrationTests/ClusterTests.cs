using RedisSlimClient.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.IntegrationTests
{
    public class ClusterTests
    {
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
                var cancel = new CancellationTokenSource(3000);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task GetAndSetString(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            using (var client = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).CreateClient())
            {
                var cancel = new CancellationTokenSource(3000);

                await client.SetStringAsync("key1", "abc");
                var result = await client.GetStringAsync("key1");
                await client.DeleteAsync("key1");

                Assert.Equal("abc", result);
            }
        }
    }
}