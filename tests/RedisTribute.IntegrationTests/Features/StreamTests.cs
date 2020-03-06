using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public async Task ReadAllAsync_SomeWriteData_CanReadAllEntries(PipelineMode pipelineMode,
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

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task ReadAsync_SomeWriteData_CanReadToPosition(PipelineMode pipelineMode,
            ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString();

                var stream = await client.GetStream<TestComplexDto>(key);
                var bagOfEvents = new ConcurrentBag<StreamEntryId>();
                var now = DateTime.UtcNow;

                var writeTasks = Enumerable.Range(1, 250).Select(async n => bagOfEvents.Add(await stream.WriteAsync(
                    new TestComplexDto()
                    {
                        DataItem1 = $"{n}",
                        DataItem2 = now.AddMinutes(n)
                    })));

                await Task.WhenAll(writeTasks);

                var sortedEvents = bagOfEvents.OrderBy(x => x).ToArray();

                var start = sortedEvents.First(); // StreamEntryId.FromUtcDateTime(now.Date);
                var middle = sortedEvents.Skip(99).First();

                _output.WriteLine(middle.Timestamp.ToDateTime().ToString("O"));

                var results = new Dictionary<StreamEntryId, TestComplexDto>();

                await stream.ReadAsync(x =>
                {
                    results.Add(x.Key, x.Value);

                    return Task.CompletedTask;
                }, start, middle, batchSize: 50);

                await stream.DeleteAsync();

                Assert.Equal(100, results.Count);
            }
        }
    }
}
