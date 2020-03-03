using RedisTribute.Configuration;
using RedisTribute.Io;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisTribute.IntegrationTests
{
    public class ConnectTests
    {
        const int defaultTimeout = 6000;
        readonly ITestOutputHelper _output;

        public ConnectTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task PingAsync_AzureConnection_ReturnsOkResult()
        {
            var config = Environments.GetAzureConfig();

            if (config != null)
            {
                using (var client = config.CreateClient())
                {
                    var results = await client.PingAllAsync();

                    Assert.All(results, x => Assert.True(x.Ok));
                }

                return;
            }
            _output.WriteLine("Azure test not configured");
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslUncontactableServer)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslUncontactableServer)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslUncontactableServer)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslUncontactableServer)]
        public async Task PingAsync_UncontactableServer_ThrowsConnectionInitialisationException(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var wasThrown = false;

                try
                {
                    await client.PingAsync(cancel.Token);
                }
                catch (ConnectionInitialisationException)
                {
                    wasThrown = true;
                }

                Assert.True(wasThrown);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        public async Task PingAsync_ViaProxy_ReturnsOk(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = await config.CreateProxiedClientAsync(r => r.ForwardResponse()))
            {
                var result = await client.PingAsync();

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        public async Task PingAsync_WithNetworkError_WillReconnect(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.FallbackStrategy = FallbackStrategy.None;

            var errorOn = false;

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                if (errorOn)
                {
                    throw new Exception();
                }

                return r.ForwardResponse();
            }))
            {
                await client.PingAllAsync();

                var compactedResults = await ExecuteMultipleRequests(client, 50, (n, r) =>
                {
                    if (n == 25)
                    {
                        errorOn = true;
                        return;
                    }
                    if (!r)
                    {
                        errorOn = false;
                        Thread.Sleep(10);
                    }
                });

                Assert.Equal(3, compactedResults.Count);
                Assert.True(compactedResults[0]);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        public async Task PingAsync_WithNetworkError_WillReconnect2(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.FallbackStrategy = FallbackStrategy.None;

            var errorOn = false;

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                if (errorOn)
                {
                    throw new Exception();
                }

                return r.ForwardResponse();
            }))
            {
                await client.PingAllAsync();

                var key = Guid.NewGuid().ToString("N").Substring(4);

                await client.SetAsync(key, $"k-{key}");

                errorOn = true;
                try
                {
                    await client.GetStringAsync(key);
                }
                catch (Exception ex)
                {
                    _output.WriteLine(ex.Message);
                }

                errorOn = false;

                var result = client.GetStringAsync(key);
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        public async Task PingAsync_WithInitialNetworkError_WillReconnect(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            config.ConnectTimeout = TimeSpan.FromSeconds(1);

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                if (r.Sequence < 2)
                {
                    throw new Exception();
                }

                return r.ForwardResponse();
            }))
            {
                var compactedResults = await ExecuteMultipleRequests(client);

                Assert.Equal(2, compactedResults.Count);
                Assert.True(compactedResults[1]);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslReplicaSetMaster)]
        public async Task PingAsync_VariousConfigurations_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslWithPassword)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslWithPassword)]
        public async Task PingAsync_RequirePassword_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.Sync, ConfigurationScenario.SslBasic)]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslReplicaSetMaster)]
        public async Task PingAllAsync_VariousConfigurations_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var results = await client.PingAllAsync(cancel.Token);

                foreach (var result in results)
                {
                    _output.WriteLine($"{result.Endpoint} {result.Ok} {result.Error}");
                }

                Assert.True(results.All(r => r.Ok));
            }
        }

        [Theory]
        [InlineData(PipelineMode.AsyncPipeline, ConfigurationScenario.NonSslReplicaSetSlave1)]
        public async Task PingAsync_Slave_ReturnsTrue(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = config.CreateClient())
            {
                var cancel = new CancellationTokenSource(defaultTimeout);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Fact]
        public async Task ConnectAsync_RemoteServer_CanSetAndGet()
        {
            var config = Environments.DefaultConfiguration(_output.WriteLine);

            using (var client = config.CreateClient())
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                var result = await client.SetAsync("key1", data);

                var data2 = await client.GetAsync("key1");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }

        [Fact]
        public async Task ConnectAsync_TwoGetCallsSameData_ReturnsTwoResults()
        {
            var config = Environments.DefaultConfiguration(_output.WriteLine);

            using (var client = config.CreateClient())
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                await client.SetAsync("key1", data);

                var data2 = await client.GetAsync("key1");
                var data3 = await client.GetAsync("key1");
                var data4 = await client.GetAsync("key1");

                var dataString2 = Encoding.ASCII.GetString(data2);
                var dataString3 = Encoding.ASCII.GetString(data3);
                var dataString4 = Encoding.ASCII.GetString(data4);

                Assert.Equal("abcdefg", dataString2);
                Assert.Equal("abcdefg", dataString3);
                Assert.Equal("abcdefg", dataString4);
            }
        }

        [Fact]
        public async Task ConnectAsync_RemoteServerMultipleThreads_CanGet()
        {
            var config = Environments.DefaultConfiguration(_output.WriteLine);

            using (var client = config.CreateClient())
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                await client.SetAsync("key1", data);

                await client.GetAsync("key1")

                    .ContinueWith(t =>
                    {
                        _output.WriteLine("Item1");

                        var dataString1 = Encoding.ASCII.GetString(t.Result);

                        Assert.Equal("abcdefg", dataString1);

                        Thread.Sleep(1000);

                        _output.WriteLine("Item1a");
                    });

                var data2 =
                    await client.GetAsync("key1");

                _output.WriteLine("Item2");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }


        private async Task<IList<bool>> ExecuteMultipleRequests(IRedisClient client, int numberOfRequests = 30, Action<int, bool> callback = null)
        {
            var results = new List<bool>();
            
            foreach (var x in Enumerable.Range(1, numberOfRequests))
            {
                try
                {
                    _output.WriteLine($"Attempting request {x}");

                    using (var cancel = new CancellationTokenSource(200))
                    {
                        var result = await client.PingAsync(cancel.Token);

                        results.Add(result);
                    }

                    _output.WriteLine("OK");

                    callback?.Invoke(x, true);
                }
                catch (Exception ex)
                {
                    results.Add(false);
                    _output.WriteLine("Error");
                    _output.WriteLine(ex.Message.ToString());
                    callback?.Invoke(x, false);
                }
            }

            var compactedResults = results.SequentialDedupe();
            return compactedResults;
        }

    }
}