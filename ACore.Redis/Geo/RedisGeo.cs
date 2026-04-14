using ACore.Abstractions.Storage.Geo;

namespace ACore.Redis.Geo;

/// <inheritdoc />
internal class RedisGeo : IStorageGeo
{
    private readonly RedisClient mClient;

    public RedisGeo(RedisClient client)
    {
        mClient = client;
    }

    /// <inheritdoc />
    public async Task Set<T>(params (string, GeoItem<T>)[] items)
    {
        foreach (var (key, geoItem) in items)
        {
            await mClient.Database.GeoAddAsync(
                key,
                geoItem.Position.Longitude,
                geoItem.Position.Latitude,
                RedisUtils.ConvertToRedisValue(geoItem.Item)
            );
        }
    }

    /// <inheritdoc />
    public async Task<float> Distance<T>(string key, T source, T destination)
    {
        var value = await mClient.Database.GeoDistanceAsync(key, RedisUtils.ConvertToRedisValue(source),
            RedisUtils.ConvertToRedisValue(destination));

        return Convert.ToSingle(value ?? default);
    }

    /// <inheritdoc />
    public async Task<GeoItem<T>[]> Radius<T>(string key, GeoPoint point, double radius)
    {
        var points = await mClient.Database.GeoRadiusAsync(key, point.Longitude, point.Latitude, radius);
        var result = new GeoItem<T>[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            result[i] = new GeoItem<T>
            {
                Item = RedisUtils.ConvertToType<T>(points[i].Member),
                Position = points[i].Position.ToGeoPoint()
            };
        }

        return result;
    }
}