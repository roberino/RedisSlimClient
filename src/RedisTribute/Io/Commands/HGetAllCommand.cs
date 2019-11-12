﻿using RedisTribute.Types;
using System;
using System.Collections.Generic;

namespace RedisTribute.Io.Commands
{
    class HGetAllCommand : RedisCommand<IDictionary<RedisKey, byte[]>>
    {
        public HGetAllCommand(RedisKey key) : base("HGETALL", key)
        {
        }

        protected override IDictionary<RedisKey, byte[]> TranslateResult(IRedisObject redisObject)
        {
            if(!(redisObject is RedisArray values))
            {
                throw new Exception("Invalid response from server");
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