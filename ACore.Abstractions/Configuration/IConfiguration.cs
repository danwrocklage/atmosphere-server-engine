namespace ACore.Abstractions;

/// <summary>
/// Common configuration getter interface
/// </summary>
public interface IConfiguration
{
    /// <summary>
    /// Get configuration by <see cref="key"/> of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="fallback">Function which is called when configuration was not found</param>
    T Get<T>(string key, Func<T> fallback = null);
    
    /// <summary>
    /// Get configuration of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="fallback">Function which is called when configuration was not found</param>
    /// <remarks>
    /// <typeparamref name="T"/> has to have <see cref="ConfigurationAttribute"/>.
    /// If it doesn't, type name will be used as configuration key
    /// </remarks>
    T Get<T>(Func<T> fallback = null);
}

/// <summary>
/// Manage configuration
/// </summary>
public interface IConfigurationManager : IConfiguration
{
    /// <summary>
    /// Add new configuration provider
    /// </summary>
    void AddProvider(IConfigurationProvider provider);
}