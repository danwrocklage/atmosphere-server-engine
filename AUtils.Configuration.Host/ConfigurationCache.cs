using System.Collections.Concurrent;
using AUtils.Configuration.Host.Models;

namespace AUtils.Configuration.Host;

internal static class ConfigurationCache
{
    private static readonly ConcurrentDictionary<string, ConfigurationResponse> sItems = new();

    public static string? Get(string role, string? environment)
    {
        if (sItems.TryGetValue(GetKey(role, environment), out var item))
            return item.Json;
        
        if (sItems.TryGetValue(GetKey(role, null), out item))
            return item.Json;
        
        return null;
    }

    public static void Invalidate(string role, string? environment)
    {
        if(!sItems.TryRemove(GetKey(role, environment), out var removedItem))
            return;

        foreach (var dependency in removedItem.Modules)
        {
            Invalidate(dependency, environment);
            Invalidate(dependency, null);
        }
    }

    public static ConfigurationResponse Add(string role, string? environment, string json, string[] modules)
    {
        var result = new ConfigurationResponse {Json = json, Modules = modules};
        sItems.AddOrUpdate(GetKey(role, environment), result, (_, _) => result);
        return result;
    }

    private static string GetKey(string role, string? environment)
    {
        if (role == null) throw new ArgumentNullException(nameof(role));
        environment ??= string.Empty;
        return $"{role}.{environment}";
    }
}