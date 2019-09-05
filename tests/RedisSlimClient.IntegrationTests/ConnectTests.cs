using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.IntegrationTests
{
    public class ConnectTests
    {
        const int defaultTimeout = 6000;
        readonly ITestOutputHelper _output;

        public ConnectTests(ITestOutputHelper output)
        {
            _output = output;
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
        public async Task PingAsync_WithNetworkError_WillReconnect(PipelineMode pipelineMode, ConfigurationScenario configurationScenario)
        {
            var config = Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine);

            using (var client = await config.CreateProxiedClientAsync(r =>
            {
                if (r.Sequence == 7)
                {
                    throw new Exception();
                }

                return r.ForwardResponse();
            }))
            {
                var results = new List<bool>();

                foreach (var x in Enumerable.Range(1, 25))
                {
                    try
                    {
                        var result = await client.PingAsync();

                        results.Add(result);

                        _output.WriteLine("OK");
                    }
                    catch (Exception ex)
                    {
                        results.Add(false);
                        _output.WriteLine(ex.Message.ToString());
                    }
                }
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

                var result = await client.SetBytesAsync("key1", data);

                var data2 = await client.GetBytesAsync("key1");

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

                await client.SetBytesAsync("key1", data);

                var data2 = await client.GetBytesAsync("key1");
                var data3 = await client.GetBytesAsync("key1");
                var data4 = await client.GetBytesAsync("key1");

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

                await client.SetBytesAsync("key1", data);

                await client.GetBytesAsync("key1")

                    .ContinueWith(t =>
                    {
                        _output.WriteLine("Item1");

                        var dataString1 = Encoding.ASCII.GetString(t.Result);

                        Assert.Equal("abcdefg", dataString1);

                        Thread.Sleep(1000);

                        _output.WriteLine("Item1a");
                    });

                var data2 =
                    await client.GetBytesAsync("key1");

                _output.WriteLine("Item2");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }
    }
}