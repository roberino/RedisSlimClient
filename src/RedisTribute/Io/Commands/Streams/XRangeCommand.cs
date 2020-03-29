using System;
using System.Collections.Generic;
using System.Text;
using RedisTribute.Types;
using RedisTribute.Types.Streams;

namespace RedisTribute.Io.Commands.Streams
{
    class XRangeCommand : RedisCommand<(StreamEntryId id, IDictionary<RedisKey, RedisKey> data)[]>
    {
        private readonly StreamEntryId _start;
        private readonly StreamEntryId _end;
        private readonly int? _count;

        // XREVRANGE : reverse

        public XRangeCommand(RedisKey key, StreamEntryId start, StreamEntryId end, int? count = null) : base("XRANGE", false, key)
        {
            _start = start;
            _end = end;
            _count = count;
        }

        protected override CommandParameters GetArgs()
        {
            if (!_count.HasValue)
            {
                return new object[]
                {
                    CommandText,
                    Key.Bytes,
                    _start.ToString(),
                    _end.ToString()
                };
            }

            return new object[]
            {
                CommandText,
                Key.Bytes,
                _start.ToString(),
                _end.ToString(),
                "COUNT",
                _count.Value.ToString()
            };
        }

        protected override (StreamEntryId id, IDictionary<RedisKey, RedisKey> data)[] TranslateResult(IRedisObject redisObject)
        {
            var arr = (RedisArray)redisObject;
            var results = new (StreamEntryId id, IDictionary<RedisKey, RedisKey> data)[arr.Count];
            var i = 0;

            foreach (var item in arr)
            {
                var parts = (RedisArray)item;

                var result = new Dictionary<RedisKey, RedisKey>();

                var id = new StreamEntryId(parts[0].ToString());

                var data = (RedisArray)parts[1];

                RedisKey currentKey = default;

                foreach (var x in data)
                {
                    if (currentKey.IsNull)
                    {
                        currentKey = x.ToKey();
                        continue;
                    }

                    result[currentKey] = x.ToKey();

                    currentKey = default;
                }

                results[i++] = (id, result);
            }

            return results;
        }
    }
}
