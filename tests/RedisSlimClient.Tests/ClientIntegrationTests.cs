using RedisSlimClient.Configuration;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Io;
using Xunit;
using Xunit.Abstractions;
using RedisSlimClient.Tests.Serialization;

namespace RedisSlimClient.Tests
{
    public class ClientIntegrationTests
    {
        readonly ITestOutputHelper _output;
        readonly Uri _localEndpoint = new Uri("tcp://localhost:6379/");

        public ClientIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Integration")]
        public async Task ConnectAsync_RemoteServer_CanPing()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var result = await client.PingAsync();

                Assert.True(result);
            }
        }

        [Fact]
        public async Task SetObjectAsync_WritesObjectDataToStream()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = new TestDto()
                {
                    DataItem1 = "y",
                    DataItem2 = DateTime.UtcNow,
                    DataItem3 = new AnotherTestDto()
                    {
                        DataItem1 = "x"
                    }
                };

                var ok = await client.SetObjectAsync("x", data);

                Assert.True(ok);

                var data2 = await client.GetObjectAsync<TestDto>("x");

                Assert.Equal("y", data2.DataItem1);
            }
        }

        [Fact(Skip = "Integration")]
        public async Task ConnectAsync_RemoteServer_CanSetAndGet()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                var result = await client.SetDataAsync("key1", data);

                var data2 = await client.GetDataAsync("key1");

                var dataString = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", dataString);
            }
        }

        [Fact(Skip = "Integration")]
        public async Task ConnectAsync_TwoGetCallsSameData_ReturnsTwoResults()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                DebugOutput.Output = s => _output.WriteLine(s);

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

        [Fact(Skip = "Integration")]
        public async Task ConnectAsync_RemoteServerMultipleThreads_CanGet()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
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
