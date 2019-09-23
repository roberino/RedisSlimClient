using RedisTribute.Types.Primatives;
using System;
using System.Buffers;
using System.Text;

namespace RedisTribute.Types
{
    readonly struct RedisString : IRedisObject, IDisposable
    {
        readonly PooledStream _dataStream;

        readonly Lazy<byte[]> _materializedBytes;

        public RedisString(byte[] value)
        {
            _dataStream = StreamPool.Instance.CreateReadonly(value);
            _materializedBytes = new Lazy<byte[]>(() => value);
        }

        public RedisString(ReadOnlySequence<byte> sequence)
        {
            var sharedStream = StreamPool.Instance.CreateReadonlyCopy(sequence);

            _dataStream = sharedStream;
            _materializedBytes = new Lazy<byte[]>(() => sharedStream.ToArray());
        }

        public byte[] Value => _materializedBytes.Value;

        public bool IsComplete => true;

        public bool IsNull => false;

        public RedisType Type => RedisType.String;

        public string ToString(Encoding encoding)
        {
            if (_dataStream.Length == 0)
            {
                return string.Empty;
            }

            return _dataStream.ToString(encoding);
        }

        public override string ToString() => ToString(Encoding.ASCII);

        public PooledStream AsStream() => _dataStream;

        public void Dispose()
        {
            _dataStream.Dispose();
        }

        public static implicit operator string(RedisString x) => x.ToString(Encoding.UTF8);
    }
}