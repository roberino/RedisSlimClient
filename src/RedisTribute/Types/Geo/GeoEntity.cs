using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Types.Geo
{
    public readonly struct GeoEntity
    {
        public GeoEntity(RedisKey key, params GeoMember[] members) : this(key, (GeoMembers)members)
        {
        }

        public GeoEntity(RedisKey key, GeoMembers members)
        {
            Key = key;
            Members = members;
        }

        public RedisKey Key { get; }

        public GeoMembers Members { get; }
    }

    public readonly struct GeoMember : IGeoSpatial
    {
        public GeoMember(RedisKey data, GeoCoordinates position)
        {
            Position = position;
            Member = data;
        }

        public GeoMember(string data, GeoCoordinates position)
        {
            Position = position;
            Member = data;
        }

        public static implicit operator GeoMember((string name, (double lon, double lat) coords) spec) => new GeoMember(spec.name, spec.coords);

        public GeoCoordinates Position { get; }
        public RedisKey Member { get; }
    }

    public sealed class GeoMembers : List<GeoMember>
    {
        public GeoMembers(IEnumerable<GeoMember> members)
        {
            AddRange(members);
        }

        public GeoMembers(params GeoMember[] members)
        {
            AddRange(members);
        }

        public static implicit operator GeoMembers((string name, (double lon, double lat) coords)[] spec) => new GeoMembers(spec.Select(m => (GeoMember)m));

        public static implicit operator GeoMembers(GeoMember[] spec) => new GeoMembers(spec);
    }
}
