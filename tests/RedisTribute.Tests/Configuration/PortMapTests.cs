using RedisTribute.Configuration;
using Xunit;

namespace RedisTribute.UnitTests.Configuration
{
    public class PortMapTests
    {
        [Fact]
        public void Map_UnknownPort_ReturnsSamePort()
        {
            var map = new PortMap();

            var port = map.Map(1234);

            Assert.Equal(1234, port);
        }

        [Fact]
        public void Map_MappedPort_ReturnsMappedPort()
        {
            var map = new PortMap().Map(1234, 4567);

            var port = map.Map(1234);

            Assert.Equal(4567, port);
        }
    }
}