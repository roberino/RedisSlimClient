using RedisTribute.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisTribute.Io.Commands
{
    class HDeleteCommand : RedisCommand<bool>
    {
        readonly RedisKey _field;

        public HDeleteCommand(RedisKey key, RedisKey field) : base("HDEL", false, key)
        {
            _field = field;
        }

        protected override bool TranslateResult(IRedisObject redisObject) => redisObject.IsOk();

        protected override CommandParameters GetArgs()
        {
            return new object[] { CommandText, Key.Bytes, _field.Bytes };
        }
    }
}
