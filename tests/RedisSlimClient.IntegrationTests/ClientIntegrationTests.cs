﻿using RedisSlimClient.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RedisSlimClient.IntegrationTests
{
    public class ClientIntegrationTests
    {
        readonly ITestOutputHelper _output;
        readonly Uri _localEndpoint = new Uri("tcp://localhost:6379/");

        public ClientIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ConnectAsync_RemoteServer_CanPing()
        {
            using (var client = RedisClient.Create(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var result = await client.PingAsync();

                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetObjectAsync_WritesObjectDataToStream(bool useAsync)
        {
            using (var client = RedisClient.Create(new ClientConfiguration(_localEndpoint.ToString())
            {
                UseAsyncronousPipeline = useAsync
            }))
            {
                var data = ObjectGeneration.CreateObjectGraph();

                var ok = await client.SetObjectAsync(data.Id, data);

                Assert.True(ok);

                var data2 = await client.GetObjectAsync<TestDtoWithGenericCollection<TestComplexDto>>(data.Id);

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

        [Fact]
        public async Task ConnectAsync_RemoteServer_CanSetAndGet()
        {
            using (var client = RedisClient.Create(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                var result = await client.SetDataAsync("key1", data);

                var data2 = await client.GetDataAsync("key1");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }

        [Fact]
        public async Task ConnectAsync_TwoGetCallsSameData_ReturnsTwoResults()
        {
            using (var client = RedisClient.Create(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                await client.SetDataAsync("key1", data);

                var data2 = await client.GetDataAsync("key1");
                var data3 = await client.GetDataAsync("key1");
                var data4 = await client.GetDataAsync("key1");

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
            using (var client = RedisClient.Create(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                await client.SetDataAsync("key1", data);

                await client.GetDataAsync("key1")
                    
                    .ContinueWith(t =>
                    {
                        _output.WriteLine("Item1");

                        var dataString1 = Encoding.ASCII.GetString(t.Result);

                        Assert.Equal("abcdefg", dataString1);

                        Thread.Sleep(1000);

                        _output.WriteLine("Item1a");
                    });

                var data2 =
                    await client.GetDataAsync("key1");

                _output.WriteLine("Item2");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }
    }
}
