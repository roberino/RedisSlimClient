using System;

namespace RedisTribute.Types.Geo
{
    /// <summary>
    /// Valid longitudes are from -180 to 180 degrees
    /// Valid latitudes are from -85.05112878 to 85.05112878 degrees
    /// </summary>
    public readonly struct GeoCoordinates : IEquatable<GeoCoordinates>
    {
        const double lonBounds = 180d;
        const double latBounds = 85.05112878d;

        public GeoCoordinates(double longitude, double latitude)
        {
            if (longitude < -lonBounds || longitude > lonBounds)
            {
                throw new ArgumentException(nameof(longitude));
            }
            if (latitude < -latBounds || latitude > latBounds)
            {
                throw new ArgumentException(nameof(latitude));
            }

            Longitude = longitude;
            Latitude = latitude;
        }

        public double Latitude { get; }

        public double Longitude { get; }

        public static implicit operator GeoCoordinates((double lon, double lat) coords) => new GeoCoordinates(coords.lon, coords.lat);

        public override bool Equals(object obj)
            => obj is GeoCoordinates ? Equals((GeoCoordinates)obj) : false;

        public bool Equals(GeoCoordinates other)
            => other.Latitude == Latitude && other.Longitude == Longitude;

        public override int GetHashCode()
            => Longitude.GetHashCode() ^ (7 * Latitude.GetHashCode());
    }
}