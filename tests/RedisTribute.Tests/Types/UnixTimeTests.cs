using RedisTribute.Types;
using System;
using Xunit;

namespace RedisTribute.UnitTests.Types
{
    public class UnixTimeTests
    {
        [Fact]
        public void ToDateTime_DefaultCtor_IsEqualToEpoch()
        {
            var timestamp = new UnixTime();

            Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), timestamp.ToDateTime());
        }

        [Fact]
        public void ToDateTime_SomeTimestamp_ReturnsCorrectDateAndTime()
        {
            var timestamp = new UnixTime(1582706566L * 1000);

            var valueAsDateTime = timestamp.ToDateTime();

            Assert.Equal(new DateTime(2020, 2, 26, 8, 42, 46, DateTimeKind.Utc), valueAsDateTime);
        }

        [Fact]
        public void GetHashCode_Int32_ReturnsValue()
        {
            var timestamp = new UnixTime(1234);

            var hash = timestamp.GetHashCode();

            Assert.Equal(1234, hash);
        }

        [Fact]
        public void GetHashCode_Int64_ReturnsModValue()
        {
            var timestamp = new UnixTime((long)int.MaxValue + 1234);

            var hash = timestamp.GetHashCode();

            Assert.Equal(1234, hash);
        }
    }
}
