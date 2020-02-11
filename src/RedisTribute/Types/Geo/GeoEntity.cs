namespace RedisTribute.Types.Geo
{
    public readonly struct GeoEntity
    {
        public GeoEntity(RedisKey key, params GeoMember[] members)
        {
            Key = key;
            Members = members;
        }

        public RedisKey Key { get; }

        public GeoMember[] Members { get; }
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
}
