namespace RedisTribute.Types.Geo
{
    public readonly struct DistanceUnit
    {
        // [m|km|ft|mi]

        private DistanceUnit(string txt) { Value = txt; }

        public bool IsDefault => Value == null;

        public string Value { get; }

        public static readonly DistanceUnit None = new DistanceUnit();
        public static readonly DistanceUnit Metres = new DistanceUnit("m");
        public static readonly DistanceUnit Kilometres = new DistanceUnit("km");
        public static readonly DistanceUnit Miles = new DistanceUnit("mi");
        public static readonly DistanceUnit Feet = new DistanceUnit("ft");
    }
}
