using ACore.Abstractions;
using ACore.Application.Configuration;

namespace ACore.Application;

/// <summary>
/// Parse and validate command line arguments
/// </summary>
internal record CommandLineArgs
{
    private static readonly HashSet<string> sSupportedConfigurations = new(StringComparer.InvariantCultureIgnoreCase)
    {
        Cell.CONFIGURATION_DEVELOPMENT,
        Cell.CONFIGURATION_STAGING,
        Cell.CONFIGURATION_PRODUCTION
    };

    public CommandLineArgs(string[] args)
    {
        if (args == null || args.Length < 2)
            throw new ArgumentException("Command line args must be (role, url, [accessToken])", nameof(args));

        DebugLogger.WriteLine("Parsing command line arguments...");

        var roleInfo = args[0].Split('.'); // name.configuration
        Role = roleInfo[0];
        Configuration = roleInfo.Length > 1 ? roleInfo[1] : sSupportedConfigurations.First();
        if (!sSupportedConfigurations.Contains(Configuration))
            throw new ConfigurationException(
                $"Invalid configuration name. Supported configurations are: {string.Join(',', sSupportedConfigurations)}");
            
        Build = roleInfo.Length > 2 ? roleInfo[2] : "Debug";
        ConfigurationPath = new Uri(args[1], UriKind.RelativeOrAbsolute);
        if (!ConfigurationPath.IsAbsoluteUri)
            ConfigurationPath =
                new Uri(new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar), ConfigurationPath);
        AccessToken = args.Length > 2 ? args[2] : string.Empty;
        
        DebugLogger.WriteLine("Command line arguments:");
        DebugLogger.WriteLine($"    {nameof(Role)}:{Role}");
        DebugLogger.WriteLine($"    {nameof(Configuration)}:{Configuration}");
        DebugLogger.WriteLine($"    {nameof(Build)}:{Build}");
        DebugLogger.WriteLine($"    {nameof(ConfigurationPath)}:{ConfigurationPath}");
    }

    /// <summary>
    /// Running application role (workloads)
    /// </summary>
    public string Role { get; }
    
    /// <summary>
    /// Type of build
    /// </summary>
    public string Configuration { get; }
    
    /// <summary>
    /// Number of build
    /// </summary>
    public string Build { get; }
    
    /// <summary>
    /// Path for get configuration data
    /// </summary>
    public Uri ConfigurationPath { get; }
    
    /// <summary>
    /// AccessToken for get configuration data (if need)
    /// </summary>
    public string AccessToken { get; }
}