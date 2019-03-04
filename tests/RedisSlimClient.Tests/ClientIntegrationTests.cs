using RedisSlimClient.Configuration;
using System;
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
    }
}
