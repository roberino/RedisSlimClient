using RedisTribute.Types;
using RedisTribute.Types.Geo;

namespace RedisTribute.Io.Commands.Geo
{
    class GeoDistCommand : RedisCommand<double>
    {
        readonly RedisKey _member1;
        readonly RedisKey _member2;
        readonly DistanceUnit _unit;

        public GeoDistCommand(RedisKey key, RedisKey member1, RedisKey member2, DistanceUnit unit = default) : base("GEODIST", false, key)
        {
            _member1 = member1;
            _member2 = member2;
            _unit = unit;
        }

        protected override CommandParameters GetArgs()
        {
            if (_unit.IsDefault)
            {
                return new object[] { CommandText, Key.Bytes, _member1.Bytes, _member2.Bytes };
            }

            return new object[] { CommandText, Key.Bytes, _member1.Bytes, _member2.Bytes, _unit.Value };
        }

        protected override double TranslateResult(IRedisObject redisObject)
        {
            return redisObject.ToDouble();
        }
    }
}
