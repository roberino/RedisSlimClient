using System;

namespace RedisTribute.Types.Geo
{
    public readonly struct GeoCoordinates : IEquatable<GeoCoordinates>
    {
        public GeoCoordinates(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }

        public double Latitude { get; }

        public double Longitude { get; }

        public override bool Equals(object obj)
            => obj is GeoCoordinates ? Equals((GeoCoordinates)obj) : false;

        public bool Equals(GeoCoordinates other)
            => other.Latitude == Latitude && other.Longitude == Longitude;

        public override int GetHashCode()
            => Longitude.GetHashCode() ^ (7 * Latitude.GetHashCode());
    }
}