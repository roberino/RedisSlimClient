using RedisTribute.Types;
using RedisTribute.Types.Geo;
using System;

namespace RedisTribute.Io.Commands.Geo
{
    class GeoAddCommand : RedisCommand<int>
    {
        readonly GeoEntity _geoEntity;

        public GeoAddCommand(GeoEntity geoEntity) : base("GEOADD", geoEntity.Key)
        {
            if (geoEntity.Members == null)
            {
                throw new ArgumentException(nameof(geoEntity.Members));
            }

            _geoEntity = geoEntity;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new object[2 + (_geoEntity.Members.Length * 3)];

            args[0] = CommandText;
            args[1] = Key;

            for (var i = 0; i < _geoEntity.Members.Length; i++)
            {
                var x = i * 3 + 2;
                args[x] = _geoEntity.Members[i].Position.Longitude.ToString();
                args[x + 1] = _geoEntity.Members[i].Position.Latitude.ToString();
                args[x + 2] = _geoEntity.Members[i].Member;
            }

            return args;
        }

        protected override int TranslateResult(IRedisObject redisObject)
        {
            return (int)redisObject.ToLong();
        }
    }
}
