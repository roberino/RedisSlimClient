using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Commands.Keys
{
    class ExpireCommand : RedisCommand<bool>
    {
        const string PExpireCommandText = "PEXPIRE";

        readonly TimeSpan _expiry;

        public ExpireCommand(RedisKey key, TimeSpan expiry) : base("EXPIRE", true, key)
        {
            _expiry = expiry;
        }

        protected override CommandParameters GetArgs()
        {
            if (_expiry.Milliseconds == 0)
            {
                return new object[] { CommandText, Key.Bytes, _expiry.TotalSeconds.ToString() };
            }

            return new object[] { PExpireCommandText, Key.Bytes, _expiry.TotalMilliseconds.ToString() };
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return redisObject.ToLong() == 1;
        }
    }
}