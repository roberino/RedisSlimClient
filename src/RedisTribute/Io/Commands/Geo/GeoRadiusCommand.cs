using RedisTribute.Types;
using RedisTribute.Types.Geo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Io.Commands.Geo
{
    class GeoRadiusCommand : RedisCommand<IDictionary<RedisKey, GeoDescriptor>>
    {
        static readonly Enum[] _allOptions = Enum.GetValues(typeof(GeoRadiusOptions)).Cast<Enum>().Skip(1).ToArray();

        readonly GeoRadiusQuery _query;
        readonly string[] _optionValues;

        public GeoRadiusCommand(GeoRadiusQuery query) : base("GEORADIUS", false, query.Key)
        {
            _query = query;
            _optionValues = _allOptions.Where(_query.Options.HasFlag).Select(o => o.ToString().ToUpper()).ToArray();
        }

        protected override CommandParameters GetArgs()
        {
            var limLen = _query.Limit.HasValue ? 1 : 0;
            var sortLen = _query.SortOrder == SortOrder.Default ? 0 : 1;
            var args = new object[6 + _optionValues.Length + limLen + sortLen];

            args[0] = CommandText;
            args[1] = Key.Bytes;
            args[2] = _query.CentrePoint.Longitude.ToString();
            args[3] = _query.CentrePoint.Latitude.ToString();
            args[4] = _query.Radius.ToString();
            args[5] = _query.Unit.IsDefault ? DistanceUnit.Metres.Value : _query.Unit.Value;

            var s = 6;

            for (var i = 0; i < _optionValues.Length; i++)
            {
                args[s++] = _optionValues[i];
            }

            if (sortLen == 1)
            {
                args[s++] = _query.SortOrder.AsRedisSortOrder();
            }

            if (limLen == 1)
            {
                args[s++] = "COUNT";
                args[s++] = _query.Limit.GetValueOrDefault().ToString();
            }

            return args;
        }

        protected override IDictionary<RedisKey, GeoDescriptor> TranslateResult(IRedisObject redisObject)
        {
            if (redisObject is RedisArray results)
            {
                if (_query.Options == GeoRadiusOptions.None)
                {
                    return results.ToDictionary(r => (RedisKey)((RedisString)r).Value, r => new GeoDescriptor());
                }

                return results.Cast<RedisArray>().ToDictionary(r => (RedisKey)((RedisString)r[0]).Value, FromArray);
            }

            throw new InvalidResponseException(redisObject);
        }

        GeoDescriptor FromArray(RedisArray value)
        {
            double? dist = null;
            GeoCoordinates? coords = null;
            string? hash = null;

            var i = 1;

            if (_query.Options.HasFlag(GeoRadiusOptions.WithDist))
            {
                dist = value[i++].ToDouble();
            }

            if (_query.Options.HasFlag(GeoRadiusOptions.WithHash))
            {
                hash = value[i++].ToString();
            }

            if (_query.Options.HasFlag(GeoRadiusOptions.WithCoord))
            {
                var coordArr = (RedisArray)value[i++];
                coords = (coordArr[0].ToDouble(), coordArr[1].ToDouble());
            }

            return new GeoDescriptor(coords, dist, hash);
        }
    }
}
