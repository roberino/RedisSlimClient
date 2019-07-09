using RedisSlimClient.Types;
using System;

namespace RedisSlimClient.Io.Commands
{
    internal class SetCommand : RedisCommand<bool>
    {
        public const string SuccessResponse = "OK";

        readonly string _key;
        readonly byte[] _data;

        public SetCommand(string key, byte[] data) : base("SET", key)
        {
            _key = key;
            _data = data;
        }

        public override object[] GetArgs() => new object[] { CommandText, _key, _data };

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), SuccessResponse, StringComparison.OrdinalIgnoreCase);
    }
}