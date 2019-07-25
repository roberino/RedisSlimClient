using RedisSlimClient.Configuration;
using System.Linq;
using Xunit;

namespace RedisSlimClient.UnitTests.Configuration
{
    public class DnsResolverTests
    {
        [Fact]
        public void Register_TwoAddresses_CanBeResolved()
        {
            var resolver = new HostAddressResolver();

            resolver
                .Register("test-host1", "192.168.0.5")
                .Register("test-host2", "192.168.0.8");

            var entry1 = resolver.Resolve("test-host1");

            Assert.Equal("test-host1", entry1.HostName);
            Assert.Equal("192.168.0.5", entry1.AddressList.Single().ToString());
        }
    }
}
