using System.Buffers;
using System.IO;
using System.Text;

namespace RedisTribute.Types
{
    readonly struct RedisString : IRedisObject
    {
        readonly ReadOnlySequence<byte> _sequence;

        public RedisString(byte[] value)
        {
            _sequence = new ReadOnlySequence<byte>(value);
        }

        public RedisString(ReadOnlySequence<byte> sequence) : this(sequence.ToArray())
        {
        }

        public byte[] Value => _sequence.ToArray();

        public bool IsComplete => true;

        public bool IsNull => false;

        public RedisType Type => RedisType.String;

        public string ToString(Encoding encoding)
        {
            if (_sequence.IsEmpty)
            {
                return null;
            }

#if NET_CORE

            if (_sequence.IsSingleSegment)
            {
                return encoding.GetString(_sequence.First.Span);
            }
#endif

            return encoding.GetString(_sequence.ToArray());
        }

        public override string ToString() => ToString(Encoding.ASCII);

        public Stream ToStream() => new MemoryStream(Value);

        public static implicit operator string(RedisString x) => x.ToString(Encoding.UTF8);
    }
}