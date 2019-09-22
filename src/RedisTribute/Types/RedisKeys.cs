using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Types
{
    class RedisKeys : IReadOnlyCollection<RedisKey>
    {
        readonly IReadOnlyCollection<RedisKey> _keys;

        public RedisKeys(IReadOnlyCollection<RedisKey> keys)
        {
            _keys = keys;
        }

        public int Count => _keys.Count;

        public IEnumerator<RedisKey> GetEnumerator() => _keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static RedisKeys FromStrings(IReadOnlyCollection<string> stringKeys) => new RedisKeys(stringKeys.Select(k => (RedisKey)k).ToList());

        public static RedisKeys FromBytes(IReadOnlyCollection<byte[]> byteKeys) => new RedisKeys(byteKeys.Select(k => (RedisKey)k).ToList());

        public static implicit operator RedisKeys(string[] stringKeys) => FromStrings(stringKeys);

        public static implicit operator RedisKeys(byte[][] byteKeys) => FromBytes(byteKeys);
    }
}