using System;

namespace RedisTribute.Types.Streams
{
    public readonly struct StreamEntryId : IEquatable<StreamEntryId>, IComparable
    {
        readonly long _id;

        public StreamEntryId(string value)
        {
            var parts = value.Split('-');

            Timestamp = new UnixTime(long.Parse(parts[0]));
            _id = long.Parse(parts[1]);
        }

        public StreamEntryId(UnixTime timestamp, long id)
        {
            Timestamp = timestamp;
            _id = id;
        }

        /// <summary>
        /// Returns the start of stream marker (-)
        /// </summary>
        public static StreamEntryId Start => new StreamEntryId(default, -1);

        /// <summary>
        /// Returns the end of stream marker (+)
        /// </summary>
        public static StreamEntryId End => new StreamEntryId(default, -2);

        /// <summary>
        /// Returns an stream entry id from a <see cref="DateTime"/> with just the timestamp portion
        /// </summary>
        public static StreamEntryId FromUtcDateTime(DateTime dateTime)
            => new StreamEntryId(UnixTime.FromUtcDateTime(dateTime), -3);

        public UnixTime Timestamp { get; }

        public long? Id => _id >= 0 ? _id : new long?();
        public bool IsStart => _id == -1;
        public bool IsEnd => _id == -2;

        public StreamEntryId Next()
        {
            if (IsStart)
            {
                return new StreamEntryId(Timestamp, 1);
            }

            if (IsEnd)
            {
                return new StreamEntryId(Timestamp.Next(), 0);
            }

            if (_id < long.MaxValue)
            {
                return new StreamEntryId(Timestamp, _id + 1);
            }

            return new StreamEntryId(Timestamp.Next(), 0);
        }

        public bool Equals(StreamEntryId other)
        {
            return Id == other.Id && Timestamp.Equals(other.Timestamp);
        }

        public int CompareTo(object obj)
        {
            if (obj is StreamEntryId other)
            {
                var tc = Timestamp.CompareTo(other.Timestamp);

                if (IsEnd)
                {
                    return other.IsEnd ? 0 : 1;
                }

                if (tc != 0 || _id == other._id || _id == -3 || other._id == -3)
                {
                    return tc;
                }

                return _id.CompareTo(other.Id);
            }

            return -1;
        }

        public static bool operator >(StreamEntryId v1, StreamEntryId v2)
        {
            return v1.CompareTo(v2) > 0;
        }

        public static bool operator <(StreamEntryId v1, StreamEntryId v2)
        {
            return v1.CompareTo(v2) < 0;
        }

        public override string ToString()
        {
            if (IsStart)
            {
                return "-";
            }

            if (IsEnd)
            {
                return "+";
            }

            if (_id == -3)
            {
                return $"{Timestamp.MillisecondTimestamp}";
            }

            return $"{Timestamp.MillisecondTimestamp}-{Id}";
        }
    }
}
