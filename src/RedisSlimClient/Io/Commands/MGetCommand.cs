using RedisSlimClient.Types;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Io.Commands
{
    class MGetCommand : RedisCommand<IEnumerable<RedisString>>, IMultiKeyCommandIdentity
    {
        readonly object[] _args;

        public MGetCommand(IReadOnlyCollection<RedisKey> keys) : base("MGET", false, default)
        {
            _args = new object[keys.Count + 1];
            _args[0] = CommandText;

            var i = 1;

            foreach (var item in keys)
            {
                _args[i++] = item.Bytes;
            }

            Keys = keys;
        }

        protected override IEnumerable<RedisString> TranslateResult(IRedisObject redisObject)
        {
            var arr = (RedisArray)redisObject;

            return arr.Cast<RedisString>();
        }

        public override object[] GetArgs() => _args;

        public IReadOnlyCollection<RedisKey> Keys { get; }
    }
}