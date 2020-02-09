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
        public GeoMember(byte[] data, GeoCoordinates position)
        {
            Position = position;
            Member = data;
        }

        public GeoCoordinates Position { get; }
        public byte[] Member { get; }
    }
}
