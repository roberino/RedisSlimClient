using RedisTribute.Types.Streams;
using System;
using System.Linq;
using RedisTribute.Types;
using Xunit;

namespace RedisTribute.UnitTests.Types.Streams
{
    public class StreamEntryIdTests
    {
        [Fact]
        public void FromUtcDateTime_SomeDateTime_TimestampIsEqualToParameter()
        {
            var now = new DateTime(1984, 6, 6, 13, 7, 9, DateTimeKind.Utc);
            var id1 = StreamEntryId.FromUtcDateTime(now);
            Assert.Equal(id1.Timestamp.ToDateTime(), now);
        }

        [Fact]
        public void OrderBy_TwoDates_EarlierDateIsFirst()
        {
            var now = DateTime.UtcNow;
            var id1 = StreamEntryId.FromUtcDateTime(now);
            var id2 = StreamEntryId.FromUtcDateTime(now.AddDays(-12));

            var first = new[] {id1, id2}.OrderBy(x => x).First();

            Assert.Equal(id2, first);
        }

        [Fact]
        public void OrderBy_ThreeDates_EarlierSequenceIsFirst()
        {
            var now = DateTime.UtcNow;

            var id1 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 2);
            var id2 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 1);
            var id3 = StreamEntryId.FromUtcDateTime(now.AddSeconds(2));

            var first = new[] { id1, id2, id3 }.OrderBy(x => x).First();

            Assert.Equal(id2, first);
        }

        [Fact]
        public void Equal_SameValue_ReturnsTrue()
        {
            var now = DateTime.UtcNow;
            var id1 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 1);
            var id2 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 1);

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void Equal_DifferentSequence_ReturnsFalse()
        {
            var now = DateTime.UtcNow;
            var id1 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 1);
            var id2 = new StreamEntryId(UnixTime.FromUtcDateTime(now), 0);

            Assert.False(id1.Equals(id2));
        }

        [Fact]
        public void GreaterThan_DifferentTimes_ReturnsExpectedResult()
        {
            var now = DateTime.UtcNow;
            var id1 = StreamEntryId.FromUtcDateTime(now.AddDays(2));
            var id2 = StreamEntryId.FromUtcDateTime(now.AddDays(7));

            Assert.True(id2 > id1);
        }

        [Fact]
        public void LessThan_DifferentTimes_ReturnsExpectedResult()
        {
            var now = DateTime.UtcNow;
            var id1 = StreamEntryId.FromUtcDateTime(now.AddDays(2));
            var id2 = StreamEntryId.FromUtcDateTime(now.AddDays(7));

            Assert.False(id2 < id1);
        }
    }
}
