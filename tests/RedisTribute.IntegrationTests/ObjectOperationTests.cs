using RedisTribute.Configuration;
using RedisTribute.Stubs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
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

                    var ok = await client.SetAsync(data.Id, data);

                    Assert.True(ok);

                    var data2 = (TestDtoWithGenericCollection<TestComplexDto>)await client.GetAsync<TestDtoWithGenericCollection<TestComplexDto>>(data.Id);

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

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task Tuple_GetSet_CanStoreAndRetrieve(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, (x : 123, y : 456, z : 789));

                var result = await client.GetAsync(key, (x: 0, y: 0, z: 0));
                var result2 = await client.GetAsync(key, (x: 0, y: 0));
                var value = result.AsValue();
                var value2 = result2.AsValue();

                Assert.Equal(123, value.x);
                Assert.Equal(456, value.y);
                Assert.Equal(789, value.z);
                Assert.Equal(123, value2.x);
                Assert.Equal(456, value2.y);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task Tuple_GetMissingValue_ReturnsDefault(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var result = await client.GetAsync(Guid.NewGuid().ToString(), (x: 987, y: 654, z: 321));

                var value = result.AsValue();

                Assert.Equal(987, value.x);
                Assert.Equal(654, value.y);
                Assert.Equal(321, value.z);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task TupleWithXElement_SetAndGet_ReturnsCorrectResult(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var key = Guid.NewGuid().ToString();

                var data = XDocument.Parse("<data id='3'>123</data>");

                await client.SetAsync(key, (name: "x", data: data.Root));

                var result = await client.GetAsync<(string name, XElement data)>(key);

                var value = result.AsValue();

                Assert.Equal("3", value.data.Attribute("id").Value);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task XDocument_Serialise_SerialisedAsXmlContent(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var someNumbers = Enumerable.Range(1, 25);
                var data = someNumbers.Select(n => new XElement("data", new XAttribute("value", n)));
                var root = new XDocument(new XElement("root", data));

                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, root);

                var xmlBytes = await client.GetAsync(key);

                using (var ms = new MemoryStream(xmlBytes))
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    var root2 = XDocument.Load(reader);

                    Assert.Equal(someNumbers.Count(), root2.Root.Elements().Count());

                    foreach (var pair in someNumbers.Zip(root2.Root.Elements(), (n, x) => (n, x)))
                        Assert.Equal(pair.n.ToString(), pair.x.Attribute("value").Value);
                }
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task Dictionary_Serialise_SerialisedAsXmlContent(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var someNumbers = Enumerable.Range(1, 25);
                var data = someNumbers.ToDictionary(n => n.ToString(), n => $"x{n}");

                var key = Guid.NewGuid().ToString();

                await client.SetAsync(key, data);

                var data2 = await client.GetAsync<Dictionary<string, string>>(key);

                foreach (var pair in someNumbers.Zip(data2.AsValue(), (n, x) => (n, x)))
                {
                    Assert.Equal(pair.n.ToString(), pair.x.Key);
                    Assert.Equal($"x{pair.n}", pair.x.Value);
                }
            }
        }
    }
}