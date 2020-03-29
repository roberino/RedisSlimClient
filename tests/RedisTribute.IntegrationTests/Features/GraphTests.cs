using System;
using System.Linq;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.IntegrationTests.Data;
using RedisTribute.Types.Graphs;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class GraphTests
    {
        readonly ITestOutputHelper _output;

        public GraphTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic2)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic2)]
        public async Task GetGraph_WhenConnected_CanQuery(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var graphNamespace = Guid.NewGuid().ToString();

                var graph = client.GetGraph<string>(graphNamespace);

                var x = await graph.GetVertexAsync("x");
                var y = await graph.GetVertexAsync("y");

                x.Label = "x";

                var edge = x.Connect(y.Id, "eq");

                await x.SaveAsync();

                var results = await x.ExecuteAsync(q =>
                    q
                    .HasLabel("x")
                    .Out("eq")
                    .Build());

                Assert.Equal(results.Single(), x);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic2)]
        public async Task CreateConnectedWordGraph_TraverseAndExport(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
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

                var graph = client.GetGraph<bool>(graphNamespace);

                IVertex<bool> last = null;

                foreach (var word in TextSample.Words().Take(300))
                {
                    if (word != ".")
                    {
                        if (word0 == null)
                        {
                            word0 = word;
                        }

                        var next = await graph.GetVertexAsync(new string(word.Where(w => char.IsLetter(w)).ToArray()));

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

                var startWord = "alice";
                var vertex0 = await graph.GetVertexAsync(startWord);
                var traversal = vertex0.Filter(q => q.In(startWord));
                var data = await traversal.ExportGmlAsync();

                _output.WriteLine(data.ToString());
            }

            _output.WriteLine($"Count: {count}");
        }
    }
}