using RedisTribute.Configuration;
using RedisTribute.Types.Graphs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class GraphTests
    {
        readonly ITestOutputHelper _output;

        public GraphTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task GetGraph_WhenConnected_CanQuery(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var graphNamespace = Guid.NewGuid().ToString();

                var graph = client.GetGraph(graphNamespace);

                var x = await graph.GetVertexAsync<string>("x");
                var y = await graph.GetVertexAsync<string>("y");

                x.Label = "x";

                var edge = x.Connect(y.Id, "eq");

                await x.SaveAsync();

                var results = await x.QueryAsync(Query<string>
                    .Create()
                    .HasLabel("x")
                    .Out("eq")
                    .Build());

                Assert.Equal(results.Single(), x);
            }
        }
    }
}