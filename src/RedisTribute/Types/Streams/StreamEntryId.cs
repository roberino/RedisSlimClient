using System;

namespace RedisTribute.Types.Streams
{
    public readonly struct StreamEntryId : IEquatable<StreamEntryId>, IComparable
    {
        public StreamEntryId(string value)
        {
            var parts = value.Split('-');

            Timestamp = new UnixTime(long.Parse(parts[0]));
            Id = long.Parse(parts[1]);
        }

        public StreamEntryId(UnixTime timestamp, long id)
        {
            Timestamp = timestamp;
            Id = id;
        }

        public static StreamEntryId End => new StreamEntryId(default, -2);

        public static StreamEntryId Start => new StreamEntryId(default, -1);

        public UnixTime Timestamp { get; }

        public long Id { get; }

        public bool IsStart => Id == -1;
        public bool IsEnd => Id == -2;

        public StreamEntryId Next()
        {
            if (Id < long.MaxValue)
            {
                return new StreamEntryId(Timestamp, Id + 1);
            }

            return new StreamEntryId(Timestamp.Next(), 0);
        }

        public bool Equals(StreamEntryId other)
        {
            return Id == other.Id && Timestamp.Equals(other.Timestamp);
        }

        public int CompareTo(object obj)
        {
            if (obj is StreamEntryId)
            {
                var other = ((StreamEntryId) obj);

                var tc = Timestamp.CompareTo(other);

                if (Id == other.Id)
                {
                    return tc;
                }

                return tc == 0 ? Id.CompareTo(other.Id) : tc;
            }

            return -1;
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

            return $"{Timestamp.MillisecondTimestamp}-{Id}";
        }
    }
}
