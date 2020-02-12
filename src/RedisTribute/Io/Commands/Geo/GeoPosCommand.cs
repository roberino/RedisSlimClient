using RedisTribute.Types;
using RedisTribute.Types.Geo;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Io.Commands.Geo
{
    class GeoPosCommand : RedisCommand<IDictionary<RedisKey, GeoCoordinates>>
    {
        readonly RedisKey[] _members;

        public GeoPosCommand(RedisKey key, params RedisKey[] members) : base("GEOPOS", key)
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

        protected override IDictionary<RedisKey, GeoCoordinates> TranslateResult(IRedisObject redisObject)
        {
            if (redisObject is RedisArray hashes)
            {
                return _members.Zip(hashes, (k, v) => (k, v)).ToDictionary(kv => kv.k, kv => TranslateCoords(kv.v));
            }

            throw new InvalidResponseException(redisObject);
        }

        GeoCoordinates TranslateCoords(IRedisObject value)
        {
            if (value is RedisArray pair)
            {
                return (pair[0].ToDouble(), pair[1].ToDouble());
            }

            throw new InvalidResponseException(value);
        }
    }
}