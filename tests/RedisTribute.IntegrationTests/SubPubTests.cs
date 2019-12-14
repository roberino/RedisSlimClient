using RedisTribute.Configuration;
using RedisTribute.Types.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

                var x = await client.PublishAsync(new Message("hey", channel));

                Assert.Equal(0, x);
            }
        }

        [Theory]
        // [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
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

                var subscription = await subClient.SubscribeAsync(new[] { channel }, m =>
                {
                    msg = m.ToString();

                    waitHandle.Set();

                    return Task.CompletedTask;
                });

                var x = await client.PublishAsync(new Message("Hey", channel));

                waitHandle.WaitOne(15000);

                await subscription.Unsubscribe();

                Assert.Equal("Hey", msg);
            }
        }
    }
}