using System;
using System.Collections.Generic;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands.Hashes
{
    class HGetAllCommand : RedisCommand<IDictionary<RedisKey, byte[]>>
    {
        public HGetAllCommand(RedisKey key) : base("HGETALL", false, key)
        {
            if (key.IsNull)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }

        protected override IDictionary<RedisKey, byte[]> TranslateResult(IRedisObject redisObject)
        {
            if(!(redisObject is RedisArray values))
            {
                throw new InvalidResponseException(redisObject);
            }

            var results = new Dictionary<RedisKey, byte[]>(values.Count);

            RedisKey key = default;

            foreach(var item in values)
            {
                using (var x = (RedisString)item)
                {
                    if (key.IsNull)
                    {
                        key = x.Value;
                        continue;
                    }
                    results[key] = x.Value;
                    key = default;
                }
            }

            return results;
        }
    }
}
