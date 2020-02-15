namespace RedisTribute.Types.Geo
{
    public readonly struct GeoRadiusQuery
    {
        public GeoRadiusQuery(
            RedisKey key,
            GeoCoordinates centrePoint,
            double radius,
            DistanceUnit distanceUnit,
            SortOrder sortOrder = SortOrder.Default,
            GeoRadiusOptions options = GeoRadiusOptions.None,
            int? limit = null)
        {
            Key = key;
            CentrePoint = centrePoint;
            Radius = radius;
            Unit = distanceUnit;
            SortOrder = sortOrder;
            Options = options;
            Limit = limit;
        }
        public RedisKey Key { get; }
        public GeoCoordinates CentrePoint { get; }
        public double Radius { get; }
        public DistanceUnit Unit { get; }
        public SortOrder SortOrder { get; }
        public GeoRadiusOptions Options { get; }
        public int? Limit { get; }
    }
}
