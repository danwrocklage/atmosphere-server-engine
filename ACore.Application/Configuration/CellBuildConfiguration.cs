namespace ACore.Application.Configuration;

/// <summary>
/// Application configuration from <see cref="IConfigurationProvider"/>
/// </summary>
public record CellBuildConfiguration
{
    /// <summary>
    /// Application role
    /// </summary>
    public string Role { get; internal init; }
    
    /// <summary>
    /// Application environment (Development, Production, etc.)
    /// </summary>
    public string Configuration { get; internal init; }
    
    /// <summary>
    /// Application build number (for CI/CD)
    /// </summary>
    public string Build { get; internal init; }
    
    /// <summary>
    /// Application modules for loading
    /// </summary>
    public string[] Modules { get; internal init; }
    
    /// <summary>
    /// Application JSON configuration
    /// </summary>
    internal string JsonPayload { get; init; }
}