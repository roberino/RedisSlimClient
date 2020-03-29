using System;

namespace RedisTribute.Types
{
    public readonly struct UnixTime : IEquatable<UnixTime>, IComparable
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public UnixTime(long millisecondTimestamp)
        {
            MillisecondTimestamp = millisecondTimestamp;
        }

        public static UnixTime UtcNow => FromUtcDateTime(DateTime.UtcNow);

        public static UnixTime FromUtcDateTime(DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"non UTC {nameof(dateTime)}: {dateTime.Kind}");
            }

            return new UnixTime((long) (dateTime - Epoch).TotalMilliseconds);
        }

        public long MillisecondTimestamp { get; }

        public UnixTime Next()
        {
            return new UnixTime(MillisecondTimestamp + 1);
        }

        public int CompareTo(object obj)
        {
            if (obj is UnixTime)
            {
                return MillisecondTimestamp.CompareTo(((UnixTime)obj).MillisecondTimestamp);
            }

            return -1;
        }

        public bool Equals(UnixTime other) => MillisecondTimestamp == other.MillisecondTimestamp;

        public override bool Equals(object obj)
        {
            if (obj is UnixTime)
            {
                return Equals((UnixTime)obj);
            }

            return false;
        }

        public override int GetHashCode() => (int)(MillisecondTimestamp % int.MaxValue);

        public DateTime ToDateTime() => Epoch.AddMilliseconds(MillisecondTimestamp);

        public byte[] ToBytes()
            => BitConverter.GetBytes(MillisecondTimestamp);
    }
}