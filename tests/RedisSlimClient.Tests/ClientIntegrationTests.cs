using RedisSlimClient.Configuration;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

        [Fact] //Skip = "Integration")]
        public async Task ConnectAsync_RemoteServer_CanSetAndGet()
        {
            using (var client = new RedisClient(new ClientConfiguration(_localEndpoint.ToString())))
            {
                var data = Encoding.ASCII.GetBytes("abcdefg");

                var result = await client.SetDataAsync("key1", data);

                var data2 = await client.GetDataAsync("key1");

                var datastr = Encoding.ASCII.GetString(data2);

                Assert.Equal("abcdefg", datastr);
            }
        }
    }
}
