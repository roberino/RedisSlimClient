using RedisTribute.Types;
using RedisTribute.Types.Streams;
using System.Collections.Generic;

namespace RedisTribute.Io.Commands.Streams
{
    class XAddCommand : RedisCommand<StreamId>
    {
        readonly IDictionary<RedisKey, RedisKey> _keyValues;

        public XAddCommand(RedisKey key, IDictionary<RedisKey, RedisKey> keyValues) : base("XADD", key)
        {
            _keyValues = keyValues;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new object[(_keyValues.Count * 2) + 3];

            args[0] = CommandText;
            args[1] = Key.Bytes;
            args[2] = "*";

            var i = 3;

            foreach (var kv in _keyValues)
            {
                args[i++] = kv.Key.Bytes;
                args[i++] = kv.Value.Bytes;
            }

            return args;
        }

        protected override StreamId TranslateResult(IRedisObject redisObject)
        {
            return new StreamId(redisObject.ToString());
        }
    }
}
