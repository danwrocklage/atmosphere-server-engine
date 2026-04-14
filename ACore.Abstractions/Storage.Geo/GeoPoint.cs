using System.Globalization;

namespace ACore.Abstractions.Storage.Geo;

/// <summary>
/// Coordinates for GEO storage
/// </summary>
public record struct GeoPoint
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public override string ToString() => 
        $"{Longitude.ToString(CultureInfo.InvariantCulture)}:{Latitude.ToString(CultureInfo.InvariantCulture)}";
}