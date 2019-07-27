using RedisSlimClient.Types;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Io.Commands
{
    internal class MGetCommand : RedisCommand<IEnumerable<RedisString>>
    {
        private readonly object[] _args;

        public MGetCommand(IReadOnlyCollection<RedisKey> keys, RedisKey key0) : base("GET", false, key0.IsNull ? keys.First() : key0)
        {
            _args = new object[keys.Count + 1];
            _args[0] = CommandText;

            var i = 1;

            foreach (var item in keys)
            {
                _args[i++] = item.Bytes;
            }
        }

        protected override IEnumerable<RedisString> TranslateResult(IRedisObject redisObject)
        {
            var arr = (RedisArray)redisObject;

            return arr.Cast<RedisString>();
        }

        public override object[] GetArgs() => _args;
    }
}