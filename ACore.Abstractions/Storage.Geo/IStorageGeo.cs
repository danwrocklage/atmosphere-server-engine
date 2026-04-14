namespace ACore.Abstractions.Storage.Geo;

/// <summary>
/// GEO based storage interface
/// </summary>
public interface IStorageGeo
{
    /// <summary>
    /// Add or update geo items
    /// </summary>
    Task Set<T>(params (string, GeoItem<T>)[] items);

    /// <summary>
    /// Get distance from 2 geo items
    /// </summary>
    Task<float> Distance<T>(string key, T source, T destination);
    
    /// <summary>
    /// Get surrounded items by circle center point and radius
    /// </summary>
    Task<GeoItem<T>[]> Radius<T>(string key, GeoPoint point, double radius);
}