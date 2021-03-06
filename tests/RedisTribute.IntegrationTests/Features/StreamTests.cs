﻿using RedisTribute.Configuration;
using RedisTribute.Stubs;
using RedisTribute.Types.Pipelines;
using RedisTribute.Types.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        // [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task CreatePipeline(PipelineMode pipelineMode,
            ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var ns = Guid.NewGuid().ToString();
                var outNs = $"{ns}/out";

                var pipeline = client
                    .CreatePipeline<TestComplexDto>(PipelineOptions.FromStartOfStream(ns, true));

                await pipeline.PushAsync(new TestComplexDto()
                {
                    DataItem1 = "a"
                });

                await pipeline.PushAsync(new TestComplexDto()
                {
                    DataItem1 = "b"
                });

                await pipeline.PushAsync(new TestComplexDto()
                {
                    DataItem1 = "xxx"
                });

                var pipeExec = pipeline
                    .Filter(x => x.Data.DataItem1?.Length == 1)
                    .Transform(x => x.DataItem1)
                    .HandleError((x, e) => Task.CompletedTask)
                    .Forward(outNs);

                await pipeExec.ExecuteAsync();

                var pipeline2 = client.CreatePipeline<string>(PipelineOptions.FromStartOfStream(outNs, true));

                var received = new ConcurrentBag<string>();

                await pipeline2
                    .Sink((x, c) =>
                    {
                        received.Add(x.Data);
                        return Task.CompletedTask;
                    })
                    .ExecuteAsync();

                Assert.Equal(2, received.Count);
                Assert.Contains("a", received);
                Assert.Contains("b", received);

                await pipeExec.DeleteAsync();
            }
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

                var stream = client.GetStream<TestComplexDto>(key);

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

                var stream = client.GetStream<TestComplexDto>(key);

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

                var stream = client.GetStream<TestComplexDto>(key);
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
