using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RedisTribute.Configuration;
using RedisTribute.Stubs;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests.Features
{
    public class JsonSerializerTests
    {
        readonly ITestOutputHelper _output;

        public JsonSerializerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        public async Task UseJsonSerialization_GetAndSetAsync_CanSerialize(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).UseJsonSerialization();

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var data = ObjectGeneration.CreateObjectGraph(5);

                var ok = await client.SetAsync(data.Id, data);

                Assert.True(ok);

                var result = await client.GetAsync<TestDtoWithGenericCollection<TestComplexDto>>(data.Id);

                var data2 = result.AsValue();

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

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        public async Task UseJsonSerialization_TupleWithXmlGetAndSetAsync_CanSerialize(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).UseJsonSerialization();

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var xml = XDocument.Parse("<data id='15'>x</data>");
                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, (x: "a", y: xml));

                var result = await client.GetAsync<(string x, XDocument y)>(key);

                var value = result.AsValue();

                Assert.Equal("a", value.x);
                Assert.Equal("15", value.y.Root.Attribute("id").Value);
            }
        }
    }
}