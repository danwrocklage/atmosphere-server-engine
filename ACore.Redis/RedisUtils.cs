using System.Text.Json;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage.Geo;
using StackExchange.Redis;

namespace ACore.Redis;

internal static class RedisUtils
{
    public static RedisValue ConvertToRedisValue<T>(T value) => JsonSerializer.Serialize(value);
    public static T ConvertToType<T>(RedisValue rv) => rv.HasValue ? JsonSerializer.Deserialize<T>((string) rv) : default;
        
    public static void LogAction<T>(this ILogger<RedisClient> logger, string action, string key, T value)
    {
        logger.Debug($"[{key}][{action}]{JsonSerializer.Serialize(value)}");
    }
        
    public static void LogAction(this ILogger<RedisClient> logger, string action, string key)
    {
        logger.Debug($"[{key}][{action}]");
    }

    public static GeoPoint ToGeoPoint(this GeoPosition position) =>
        new()
        {
            Longitude = Convert.ToSingle(position.Longitude),
            Latitude = Convert.ToSingle(position.Latitude)
        };

    public static GeoPoint ToGeoPoint(this GeoPosition? position) => 
        position?.ToGeoPoint() ?? new GeoPoint();
}