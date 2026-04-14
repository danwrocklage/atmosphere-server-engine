namespace AUtils.Configuration.Host.Models;

/// <summary>
/// Response configuration for cell
/// </summary>
public class ConfigurationResponse
{
    /// <summary>
    /// Configuration itself
    /// </summary>
    public string? Json { get; set; }

    /// <summary>
    /// Dependency modules
    /// </summary>
    public string[] Modules { get; set; } = Array.Empty<string>();
}