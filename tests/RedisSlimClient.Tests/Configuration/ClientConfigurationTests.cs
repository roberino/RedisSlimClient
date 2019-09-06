using RedisSlimClient.Configuration;
using System.Linq;
using System.Text;
using Xunit;

namespace RedisSlimClient.UnitTests.Configuration
{
    public class ClientConfigurationTests
    {
        [Fact]
        public void Ctr_BasicConfigWithSemiColonDelimiter_ReturnsValidConfiguration()
        {
            var config = new ClientConfiguration("host1:1234,ClientName=client1");

            Assert.Equal("host1", config.ServerEndpoints.Single().Host);
            Assert.Equal(1234, config.ServerEndpoints.Single().Port);
            Assert.Equal("client1", config.ClientName);
        }

        [Fact]
        public void Ctr_BasicConfigWithCommaDelimiter_ReturnsValidConfiguration()
        {
            var config = new ClientConfiguration("host1:1234;ClientName=client1");

            Assert.Equal("host1", config.ServerEndpoints.Single().Host);
            Assert.Equal(1234, config.ServerEndpoints.Single().Port);
            Assert.Equal("client1", config.ClientName);
        }

        [Fact]
        public void Ctr_AdditionalDelimiter_ReturnsValidConfiguration()
        {
            var config = new ClientConfiguration("host1:1234;ClientName=client1;;");

            Assert.Equal("host1", config.ServerEndpoints.Single().Host);
        }

        [Fact]
        public void Ctr_BasicConfigWithMultipleHosts_ReturnsMultipleServerEndpoints()
        {
            var config = new ClientConfiguration("host1:1234|host2:4567;ClientName=client1");

            Assert.Equal("host1", config.ServerEndpoints.First().Host);
            Assert.Equal(1234, config.ServerEndpoints.First().Port);
            Assert.Equal("host2", config.ServerEndpoints.ElementAt(1).Host);
            Assert.Equal(4567, config.ServerEndpoints.ElementAt(1).Port);
            Assert.Equal("client1", config.ClientName);
        }

        [Fact]
        public void ToString_BasicConfigWithMultipleHosts_ReturnsValidConfigString()
        {
            var config = new ClientConfiguration("host1:1234|host2:4567;ClientName=client1");

            var configStr = config.ToString();

            var config2 = new ClientConfiguration(configStr);
        }

        [Fact]
        public void ToString_ConfigWithEncoding_ReturnsValidEncoding()
        {
            var config = new ClientConfiguration("host1:1234|host2:4567;ClientName=client1")
            {
                Encoding = Encoding.GetEncoding("ISO-8859-1")
            };

            var configStr = config.ToString();

            var config2 = new ClientConfiguration(configStr);

            Assert.Equal(config.Encoding.CodePage, config2.Encoding.CodePage);
        }
    }
}