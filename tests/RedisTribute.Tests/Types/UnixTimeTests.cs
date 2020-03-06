using RedisTribute.Types;
using System;
using System.Linq;
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
        public void OrderBy_ThreeDifferentTimes_SortsEarliestFirst()
        {
            var now = DateTime.UtcNow;

            var timestamp1 = UnixTime.FromUtcDateTime(now.AddDays(1));
            var timestamp2 = UnixTime.FromUtcDateTime(now.AddDays(-2));
            var timestamp3 = UnixTime.FromUtcDateTime(now);

            var ordered = new[] {timestamp1, timestamp2, timestamp3}.OrderBy(x => x).ToArray();

            Assert.Equal(timestamp2, ordered[0]);
            Assert.Equal(timestamp3, ordered[1]);
            Assert.Equal(timestamp1, ordered[2]);
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
