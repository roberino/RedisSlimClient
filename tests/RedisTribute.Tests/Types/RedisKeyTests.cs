using RedisTribute.Types;
using Xunit;

namespace RedisTribute.UnitTests.Types
{
    public class RedisKeyTests
    {
        [Fact]
        public void GetHashCode_SameString_ReturnsSameHash()
        {
            var key1 = (RedisKey)"abc";
            var key2 = (RedisKey)"abc";

            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferntString_ReturnsDifferentHash()
        {
            var key1 = (RedisKey)"abc";
            var key2 = (RedisKey)"abcd";

            Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
        }

        [Fact]
        public void Equals_SameString_ReturnsTrue()
        {
            var key1 = (RedisKey)"abc";
            var key2 = (RedisKey)"abc";

            Assert.True(key1.Equals(key2));
        }
    }
}
