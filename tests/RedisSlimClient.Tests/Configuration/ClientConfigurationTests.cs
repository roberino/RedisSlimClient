using RedisSlimClient.Configuration;
using System.Linq;
using Xunit;

namespace RedisSlimClient.UnitTests.Configuration
{
    public class ClientConfigurationTests
    {
        [Fact]
        public void Ctr_BasicConfig_ReturnsValidConfiguration()
        {
            var config = new ClientConfiguration("host1:1234,ClientName=client1");

            Assert.Equal("host1", config.ServerEndpoints.Single().Host);
            Assert.Equal(1234, config.ServerEndpoints.Single().Port);
            Assert.Equal("client1", config.ClientName);
        }
    }
}