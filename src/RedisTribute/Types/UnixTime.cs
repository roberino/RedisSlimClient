using System;

namespace RedisTribute.Types
{
    public readonly struct UnixTime : IEquatable<UnixTime>, IComparable
    {
        static readonly DateTime _expoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public UnixTime(long millisecondTimestamp)
        {
            MillisecondTimestamp = millisecondTimestamp;
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

        public DateTime ToDateTime() => _expoch.AddMilliseconds(MillisecondTimestamp);
    }
}