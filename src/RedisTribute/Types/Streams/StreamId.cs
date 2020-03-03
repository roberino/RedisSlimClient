using System;

namespace RedisTribute.Types.Streams
{
    public readonly struct StreamId : IEquatable<StreamId>, IComparable
    {
        public StreamId(string value)
        {
            var parts = value.Split('-');

            Timestamp = new UnixTime(long.Parse(parts[0]));
            Id = int.Parse(parts[0]);
        }

        public UnixTime Timestamp { get; }

        public int Id { get; }

        public bool Equals(StreamId other)
        {
            return Id == other.Id && Timestamp.Equals(other.Timestamp);
        }

        public int CompareTo(object obj)
        {
            if (obj is StreamId)
            {
                var other = ((StreamId) obj);

                var tc = Timestamp.CompareTo(other);

                if (Id == other.Id)
                {
                    return tc;
                }

                return tc == 0 ? Id.CompareTo(other.Id) : tc;
            }

            return -1;
        }
    }
}
