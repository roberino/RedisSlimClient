using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;

namespace RedisSlimClient.Io.Server
{
    class ClientSetNameCommand : RedisCommand<bool>
    {
        readonly string _name;

        public ClientSetNameCommand(string name) : base("CLIENT")
        {
            _name = name;
        }

        public override object[] GetArgs() => new[] { CommandText, "SETNAME", _name };

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", StringComparison.OrdinalIgnoreCase);
        }
    }
}