using RedisTribute.Configuration;
using RedisTribute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using RedisTribute.Stubs;
using System.Collections.Generic;
using RedisTribute.Types.Messaging;
using System.Collections.Concurrent;

namespace RedisTribute.IntegrationTests
{
    public class SubPubTests
    {
        readonly ITestOutputHelper _output;

        public SubPubTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task PublishAsync_NoSubscribers_ReturnsZero(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAllAsync();

                var channel = Guid.NewGuid().ToString().Substring(0, 6);

                var x = await client.PublishStringAsync(channel, "hey");

                Assert.Equal(0, x);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task PublishAsync_OneSubscriber_SubscriberReceivesMessage(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            using (var subClient = config.CreateSubscriberClient())
            using (var waitHandle = new ManualResetEvent(false))
            {
                await client.PingAllAsync();
                await subClient.PingAllAsync();

                var msg = string.Empty;

                var channel = Guid.NewGuid().ToString().Substring(0, 6);

                var subscription = await subClient.SubscribeAsync(channel, m =>
                {
                    msg = m.ToString();

                    waitHandle.Set();

                    return Task.CompletedTask;
                });

                var x = await client.PublishStringAsync(channel, "Hey");

                waitHandle.WaitOne(15000);

                await subscription.Unsubscribe();

                Assert.Equal("Hey", msg);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslClusterSet)]
        public async Task PublishAsync_OneSubscriberTwoChannels_SubscriberReceivesMessage(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            using (var subClient = config.CreateSubscriberClient())
            using (var waitHandle = new ManualResetEvent(false))
            {
                await client.PingAllAsync();
                await subClient.PingAllAsync();

                var msg = new ConcurrentDictionary<string, string>();

                var channel1 = Guid.NewGuid().ToString().Substring(0, 6);
                var channel2 = Guid.NewGuid().ToString().Substring(0, 6);
                var count = 0;

                var subscription = await subClient.SubscribeAsync(new[] { channel1, channel2 }, m =>
                {
                    msg[m.Channel] = m.ToString();

                    if (Interlocked.Increment(ref count) == 2)
                    {
                        waitHandle.Set();
                    }

                    return Task.CompletedTask;
                });

                await Task.WhenAll(
                    client.PublishStringAsync(channel1, "Hey"), 
                    client.PublishStringAsync(channel2, "You"));

                waitHandle.WaitOne(3000);

                await subscription.Unsubscribe();

                Assert.Equal("Hey", msg[channel1]);
                Assert.Equal("You", msg[channel2]);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task PublishAsync_ObjectMessage_SubscriberReceivesMessage(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            using (var subClient = config.CreateSubscriberClient())
            using (var waitHandle = new ManualResetEvent(false))
            {
                await client.PingAllAsync();
                await subClient.PingAllAsync();

                var msg = string.Empty;

                var channel = Guid.NewGuid().ToString().Substring(0, 6);

                var subscription = await subClient.SubscribeAsync<TestComplexDto>(channel, m =>
                {
                    msg = m.Body.DataItem1;

                    waitHandle.Set();

                    return Task.CompletedTask;
                });

                var x = await client.PublishAsync(channel, new TestComplexDto()
                {
                     DataItem1 = "Hey"
                }, new Dictionary<string, object>()
                {
                    ["Header1"] = 1234
                });

                waitHandle.WaitOne(15000);

                await subscription.Unsubscribe();

                Assert.Equal("Hey", msg);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task PublishAsync_ObjectMessageTwoSubscribers_SubscriberReceivesMessage(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            using (var subClient1 = config.CreateSubscriberClient())
            using (var subClient2 = config.CreateSubscriberClient())
            using (var waitHandle1 = new ManualResetEvent(false))
            using (var waitHandle2 = new ManualResetEvent(false))
            {
                await client.PingAllAsync();
                await subClient1.PingAllAsync();

                var msg1 = string.Empty;
                var msg2 = string.Empty;

                var channel = Guid.NewGuid().ToString().Substring(0, 6);

                var subscription1 = await subClient1.SubscribeAsync<TestComplexDto>(channel, m =>
                {
                    msg1 = m.Body.DataItem1;

                    waitHandle1.Set();

                    return Task.CompletedTask;
                });

                var subscription2 = await subClient2.SubscribeAsync<TestComplexDto>(channel, m =>
                {
                    msg2 = m.Body.DataItem1;

                    waitHandle2.Set();

                    return Task.CompletedTask;
                });

                var x = await client.PublishAsync(channel, new TestComplexDto()
                {
                    DataItem1 = "Hey"
                });

                waitHandle1.WaitOne(5000);
                waitHandle2.WaitOne(5000);

                await subscription1.Unsubscribe();
                await subscription2.Unsubscribe();

                Assert.Equal("Hey", msg1);
                Assert.Equal("Hey", msg2);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task PublishAsync_ObjectMessageTwoSubscribersWithLock_SubscriberReceivesMessage(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            using (var subClient1 = config.CreateSubscriberClient())
            using (var subClient2 = config.CreateSubscriberClient())
            using (var waitHandle1 = new ManualResetEvent(false))
            using (var waitHandle2 = new ManualResetEvent(false))
            {
                await client.PingAllAsync();
                await subClient1.PingAllAsync();

                string msg1 = null, msg2 = null;

                var channel = Guid.NewGuid().ToString().Substring(0, 6);

                var subscription1 = await subClient1.SubscribeAsync<TestComplexDto>(channel, m =>
                {
                    msg1 = m.Body.DataItem1;

                    waitHandle1.Set();

                    return Task.CompletedTask;
                });

                var subscription2 = await subClient2.SubscribeAsync<TestComplexDto>(channel, m =>
                {
                    msg2 = m.Body.DataItem1;

                    waitHandle2.Set();

                    return Task.CompletedTask;
                });

                var x = await client.PublishAsync(channel, new TestComplexDto()
                {
                    DataItem1 = "Hey"
                }, flags: MessageFlags.SingleConsumer);


                var count = 0;

                while (msg1 == null && msg2 == null && count++ < 1000)
                {
                    waitHandle1.WaitOne(10);
                    waitHandle2.WaitOne(10);
                }

                await subscription1.Unsubscribe();
                await subscription2.Unsubscribe();

                Assert.Equal("Hey", $"{msg1}{msg2}");
            }
        }
    }
}