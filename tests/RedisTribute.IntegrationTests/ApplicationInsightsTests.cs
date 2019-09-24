using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using RedisTribute.ApplicationInsights;
using RedisTribute.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class ApplicationInsightsTests
    {
        readonly ITestOutputHelper _output;

        public ApplicationInsightsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        public async Task UseApplicationInsights_GetAndSetAsync_CreatesDependencyTelemetry(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var appInsightsConfig = new TelemetryConfiguration("x");
            var channel = new StubTelemetryChannel();

            appInsightsConfig.TelemetryChannel = channel;

            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine).UseApplicationInsights(appInsightsConfig);

            var key = Guid.NewGuid().ToString();

            using (var client = config.CreateClient())
            {
                await client.SetAsync(key, "abc");
                await client.GetAsync(key);
            }

            var telemetryItems = channel.Where(t => t is DependencyTelemetry).Cast<DependencyTelemetry>().ToList();
            var telemetry1 = telemetryItems.FirstOrDefault();

            Assert.True(telemetryItems.Count > 1);
            Assert.All(telemetryItems, x =>
            {
                Assert.True(x.Success);
                Assert.NotNull(x.Target);
                Assert.Equal("REDIS", x.Type);
            });

            Assert.NotNull(telemetryItems.Single(t => t.Data == $"SET/{key}"));
            Assert.NotNull(telemetryItems.Single(t => t.Data == $"GET/{key}"));
        }

        class StubTelemetryChannel : List<ITelemetry>, ITelemetryChannel
        {
            public bool? DeveloperMode { get; set; } = false;
            public string EndpointAddress { get; set; } = "localhost";

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void Send(ITelemetry item)
            {
                Add(item);
            }
        }
    }
}
