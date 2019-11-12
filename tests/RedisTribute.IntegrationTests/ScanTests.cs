using RedisTribute.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class ScanTests
    {
        readonly ITestOutputHelper _output;

        public ScanTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        public async Task ScanKeysAsync_SomePattern_ReturnsExpectedResults(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine, 5);

            config.HealthCheckInterval = TimeSpan.Zero;

            using (var client = config.CreateClient())
            {
                await client.PingAsync();

                var prefix = Guid.NewGuid().ToString().Substring(8);

                var keys = Enumerable.Range(1, 25).Select(n => $"{prefix}-{n}-{Guid.NewGuid().ToString().Substring(0, 6)}").ToList();

                foreach (var key in keys)
                {
                    await client.SetAsync(key, new byte[] { 1, 2, 3 });
                }

                var results = new List<string>();

                await client.ScanKeysAsync(new ScanOptions(k =>
                {
                    results.Add(k);
                    return Task.CompletedTask;
                }, $"{prefix}*"));

                Assert.Equal(25, results.Count);
            }
        }
    }
}