namespace ACore.Application.Configuration;

/// <summary>
/// Application configuration provider interface 
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Create configuration provider based on command line arguments
    /// </summary>
    public static IConfigurationProvider Create(string[] args)
    {
        DebugLogger.WriteLine("Create configuration provider...");
        
        var argsInfo = new CommandLineArgs(args);

        return argsInfo.ConfigurationPath.Scheme switch
        {
            "file" => new FileConfigurationProvider(argsInfo),
            "http" => new HttpConfigurationProvider(argsInfo),
            _ => throw new ConfigurationException($"Url scheme {argsInfo.ConfigurationPath.Scheme} is not supported")
        };
    }

    /// <summary>
    /// Get application configuration from source
    /// </summary>
    Task<CellBuildConfiguration> Get(CancellationToken token = default);
}