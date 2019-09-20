using RedisTribute.Io.Commands;
using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Server
{
    class ClientSetNameCommand : RedisCommand<bool>
    {
        readonly string _name;

        public ClientSetNameCommand(string name) : base("CLIENT")
        {
            _name = name;
        }

        protected override CommandParameters GetArgs() => new[] { CommandText, "SETNAME", _name };

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", StringComparison.OrdinalIgnoreCase);
        }
    }
}