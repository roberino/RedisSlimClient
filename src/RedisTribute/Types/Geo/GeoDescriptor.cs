namespace RedisTribute.Types.Geo
{
    public readonly struct GeoDescriptor
    {
        public GeoDescriptor(GeoCoordinates? coords, double? distance, string hash)
        {
            Position = coords;
            Distance = distance;
            Hash = hash;
        }

        public bool IsEmpty => !Position.HasValue && !Distance.HasValue && Hash == null;

        public GeoCoordinates? Position { get; }

        public double? Distance { get; }

        public string Hash { get; }
    }
}
