using RedisSlimClient.Configuration;
using RedisSlimClient.Stubs;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.IntegrationTests
{
    public class ObjectOperationTests
    {
        readonly ITestOutputHelper _output;

        public ObjectOperationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic, 5)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic, 5)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic, 10)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic, 10)]
        public async Task MultipleOperations_MultipleIterations_ExecutesSuccessfully(PipelineMode pipelineMode, ConfigurationScenario configurationScenario, int iterations)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                foreach (var n in Enumerable.Range(1, iterations))
                {
                    var data = ObjectGeneration.CreateObjectGraph(5);

                    var ok = await client.SetObjectAsync(data.Id, data);

                    Assert.True(ok);

                    var data2 = await client.GetObjectAsync<TestDtoWithGenericCollection<TestComplexDto>>(data.Id);

                    Assert.Equal(data.Id, data2.Id);
                    Assert.Equal(data.Items.Count, data2.Items.Count);

                    foreach (var x in data.Items.Zip(data2.Items, (a, b) => (a, b)))
                    {
                        Assert.Equal(x.a.DataItem1, x.b.DataItem1);
                        Assert.Equal(x.a.DataItem2, x.b.DataItem2);
                        Assert.Equal(x.a.DataItem3.DataItem1, x.b.DataItem3.DataItem1);
                    }

                    var deleted = await client.DeleteAsync(data.Id);

                    Assert.Equal(1, deleted);
                }
            }
        } 
    }
}