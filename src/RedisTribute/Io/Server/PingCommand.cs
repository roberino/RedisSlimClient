using RedisTribute.Io.Commands;
using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Server
{
    class PingCommand : RedisCommand<bool>
    {
        public const string SuccessResponse = "PONG";

        public PingCommand() : base("PING", false, default)
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), SuccessResponse, StringComparison.OrdinalIgnoreCase);
    }
}