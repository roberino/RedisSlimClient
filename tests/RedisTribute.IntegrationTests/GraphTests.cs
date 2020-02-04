using RedisTribute.Configuration;
using RedisTribute.Types.Graphs;
using RedisTribute.Util;
using System;
using System.Linq;
using System.Text;
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

                var results = await x.ExecuteAsync(Query<string>
                    .Create()
                    .HasLabel("x")
                    .Out("eq")
                    .Build());

                Assert.Equal(results.Single(), x);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task CreateConnectedGraph(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode);

            config.HealthCheckInterval = TimeSpan.Zero;
            config.FallbackStrategy = FallbackStrategy.None;

            string word0 = null;
            var count = 0;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var graphNamespace = Guid.NewGuid().ToString();

                var graph = client.GetGraph(graphNamespace);

                IVertex<bool> last = null;

                foreach (var word in TextSample.Words().Take(3000))
                {
                    //_output.WriteLine(word);

                    if (word != ".")
                    {
                        if (word0 == null)
                        {
                            word0 = word;
                        }

                        var next = await graph.GetVertexAsync<bool>(new string(word.Where(w => char.IsLetter(w)).ToArray()));

                        next.Label = word;

                        if (last != null)
                        {
                            last.Connect(next.Id, direction: Direction.Out);

                            await last.SaveAsync();
                        }

                        last = next;
                    }
                    else
                    {
                        if (last != null)
                        {
                            await last.SaveAsync();

                            last = null;
                        }
                    }

                    count++;
                }

                var vertex0 = await graph.GetVertexAsync<bool>(word0);
                var traversal = vertex0.ApplyQuery(Query<bool>.Create().In("i"));
                var data = await vertex0.ExportGmlAsync();

                data.Save("c:\\dev\\query.xml");
            }

            _output.WriteLine($"Count: {count}");
        }
    }
}