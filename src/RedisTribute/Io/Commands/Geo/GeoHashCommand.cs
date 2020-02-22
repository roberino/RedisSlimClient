using RedisTribute.Types;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Io.Commands.Geo
{
    class GeoHashCommand : RedisCommand<IDictionary<RedisKey, string>>
    {
        //See http://geohash.org/

        readonly RedisKey[] _members;

        public GeoHashCommand(RedisKey key, params RedisKey[] members) : base("GEOHASH", false, key)
        {
            _members = members;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new object[_members.Length + 2];

            args[0] = CommandText;
            args[1] = Key.Bytes;

            for (var i = 0; i < _members.Length; i++)
            {
                args[i + 2] = _members[i].Bytes;
            }

            return args;
        }

        protected override IDictionary<RedisKey, string> TranslateResult(IRedisObject redisObject)
        {
            if (redisObject is RedisArray hashes)
            {
                return _members.Zip(hashes, (k, v) => (k, v)).ToDictionary(kv => kv.k, kv => kv.v.ToString());
            }

            throw new InvalidResponseException(redisObject);
        }
    }
}
