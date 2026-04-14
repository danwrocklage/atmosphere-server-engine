using System.Collections.Concurrent;
using System.Text.Json;
using ACore.Abstractions;

namespace ACore.Configuration.Providers;

internal class EnvVarConfigurationProvider : IConfigurationProvider
{
    private static readonly ConcurrentDictionary<string, string> sEnvVarCache = new();

    public bool IsExists(string key) => !string.IsNullOrEmpty(GetInternal(key));

    public (T, bool) Get<T>(string key)
    {
        var value = GetInternal(key);
        if (string.IsNullOrEmpty(value))
            return (default, false);
        
        return (typeof(T).Namespace == "System" ? 
            (T) Convert.ChangeType(value, typeof(T)) : 
            JsonSerializer.Deserialize<T>(value), true);
    }
    
    private static string GetInternal(string key)
    {
        if (sEnvVarCache.TryGetValue(key, out var value)) 
            return value;
        
        value = Environment.GetEnvironmentVariable(key);
        sEnvVarCache.TryAdd(key, value);
        return value;
    }
}