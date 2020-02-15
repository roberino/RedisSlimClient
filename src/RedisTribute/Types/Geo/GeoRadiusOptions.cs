using System;

namespace RedisTribute.Types.Geo
{
    [Flags]
    public enum GeoRadiusOptions : byte
    {
        None = 0,
        WithCoord = 1,
        WithDist = 2,
        WithHash = 4
    }
}
