﻿using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.IntegrationTests
{
    public class BasicTests
    {
        readonly ITestOutputHelper _output;

        public BasicTests(ITestOutputHelper output)
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
            using (var client = RedisClient.Create(Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine)))
            {
                var cancel = new CancellationTokenSource(3000);
                var wasThrown = false;

                try
                {
                    var result = await client.PingAsync(cancel.Token);
                }
                catch (ConnectionInitialisationException)
                {
                    wasThrown = true;
                }

                Assert.True(wasThrown);
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
            using (var client = RedisClient.Create(Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine)))
            {
                var cancel = new CancellationTokenSource(3000);
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
            using (var client = RedisClient.Create(Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine)))
            {
                var cancel = new CancellationTokenSource(3000);
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
            using (var client = RedisClient.Create(Environments.GetConfiguration(configurationScenario, pipelineMode, _output.WriteLine)))
            {
                var cancel = new CancellationTokenSource(3000);
                var result = await client.PingAsync(cancel.Token);

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(4, 50)]
        public async Task PingAsync_MutlipleThreads_ReturnsTrue(int maxThreads, int iterations)
        {
            using (var client = RedisClient.Create(Environments.DefaultConfiguration(_output.WriteLine)))
            {
                var success = false;
                var ev = new ManualResetEvent(false);

                await client.PingAsync();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    foreach (var i in Enumerable.Range(1, iterations))
                    {
                        var tasks = Enumerable.Range(1, maxThreads)
                            .Select(async n =>
                            {
                                var result = await client.PingAsync();

                                Assert.True(result);
                            }).ToList();

                        Task.WhenAll(tasks).Wait();
                    }

                    success = true;

                    ev.Set();
                });

                ev.WaitOne(2500);

                ev.Dispose();

                Assert.True(success);
            }
        }

        [Fact]
        public async Task ConnectAsync_RemoteServer_CanSetAndGet()
        {
            using (var client = RedisClient.Create(Environments.DefaultConfiguration(_output.WriteLine)))
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
            using (var client = RedisClient.Create(Environments.DefaultConfiguration(_output.WriteLine)))
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
            using (var client = RedisClient.Create(Environments.DefaultConfiguration(_output.WriteLine)))
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