using RedisSlimClient.Types;
using System;

namespace RedisSlimClient.Io.Commands
{
    internal class SetCommand : RedisCommand<bool>
    {
        public const string SuccessResponse = "OK";

        readonly byte[] _data;

        public SetCommand(RedisKey key, byte[] data) : base("SET", true, key)
        {
            _data = data;
        }

        public override object[] GetArgs() => new object[] { CommandText, Key.Bytes, _data };

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), SuccessResponse, StringComparison.OrdinalIgnoreCase);
    }
}