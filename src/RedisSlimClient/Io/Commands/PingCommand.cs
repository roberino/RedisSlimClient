﻿using RedisSlimClient.Types;
using System;

namespace RedisSlimClient.Io.Commands
{
    internal class PingCommand : RedisCommand<bool>
    {
        public const string SuccessResponse = "PONG";

        public PingCommand() : base("PING")
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject) => string.Equals(redisObject.ToString(), SuccessResponse, StringComparison.OrdinalIgnoreCase);
    }
}