using System.Reflection;
using ACore.Abstractions;
using ACore.Application;

namespace ACore.Configuration;

/// <inheritdoc cref="IConfiguration"/>
internal class Configuration : IConfigurationManager
{
    private static readonly Dictionary<Type, string> sConfigurationsNames = new();
    private readonly List<IConfigurationProvider> mProviders;

    public Configuration(IEnumerable<IConfigurationProvider> providers)
    {
        mProviders = providers.ToList();
        DebugLogger.WriteLine($"There are {mProviders.Count} {nameof(IConfigurationProvider)}");
    }
    
    /// <inheritdoc/>
    public T Get<T>(string key, Func<T> fallback = null)
    {
        foreach (var provider in mProviders)
        {
            if(!provider.IsExists(key))
                continue;

            var (value, isValueGot) = provider.Get<T>(key);
            return isValueGot ? value : fallback == null ? default : fallback();
        }

        return fallback == null ? default : fallback();
    }

    /// <inheritdoc/>
    public T Get<T>(Func<T> fallback = null) => Get(GetName<T>(), fallback);

    /// <inheritdoc/>
    public void AddProvider(IConfigurationProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        mProviders.Add(provider);
        DebugLogger.WriteLine($"A new {nameof(IConfigurationProvider)} was added ({provider.GetType().FullName})");
    }
    
    private static string GetName<T>()
    {
        var type = typeof(T);
        if (sConfigurationsNames.TryGetValue(type, out var key)) 
            return key;
            
        var attr = type.GetCustomAttribute<ConfigurationAttribute>();
        key = attr?.Name ?? type.FullName ?? type.Name;
        sConfigurationsNames.Add(type, key);

        return key;
    }
}