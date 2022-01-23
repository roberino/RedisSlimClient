using System;
using System.Collections;
using System.Text;

namespace RedisTribute.Types
{
    public readonly struct RedisKey : IEquatable<RedisKey>
    {
        RedisKey(byte[] bytes)
        {
            Bytes = bytes;
        }

        public bool IsNull => Bytes == null;

        public byte[] Bytes { get; }

        public static implicit operator RedisKey(string x) => new RedisKey(Encoding.UTF8.GetBytes(x));

        public static implicit operator RedisKey(byte[] x) => new RedisKey(x);

        public static implicit operator byte[] (RedisKey x) => x.Bytes;

        public override string ToString() => IsNull ? string.Empty : Encoding.UTF8.GetString(Bytes);

        public bool Equals(RedisKey other)
        {
            if (other.Bytes == null || Bytes == null)
            {
                return false;
            }

            if (other.Bytes.Length != Bytes.Length)
            {
                return false;
            }

            return StructuralComparisons.StructuralEqualityComparer.Equals(Bytes, other.Bytes);
        }

        public override bool Equals(object? obj)
        {
            if (obj is RedisKey)
            {
                return base.Equals((RedisKey)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Bytes == null ? 0 : StructuralComparisons.StructuralEqualityComparer.GetHashCode(Bytes);
        }
    }
}