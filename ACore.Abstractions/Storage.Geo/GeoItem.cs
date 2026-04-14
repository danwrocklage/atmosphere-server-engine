namespace ACore.Abstractions.Storage.Geo;

/// <summary>
/// GEO storage item with coordinates
/// </summary>
public struct GeoItem<T>
{
    public GeoPoint Position { get; set; }
        
    public T Item { get; set; }
}