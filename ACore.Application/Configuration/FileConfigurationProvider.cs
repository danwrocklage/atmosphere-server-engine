using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACore.Application.Configuration;

/// <summary>
/// Get application configuration from JSON file
/// </summary>
internal class FileConfigurationProvider : IConfigurationProvider
{
    private const string INCLUDES_KEY = "includes";
    private const string MODULES_KEY = "modules";

    private readonly string mConfigurationFile;
    private readonly CommandLineArgs mArgsInfo;

    public FileConfigurationProvider(CommandLineArgs argsInfo)
    {
        DebugLogger.WriteLine("Use file configuration provider");

        mArgsInfo = argsInfo;

        var path = mArgsInfo.ConfigurationPath.LocalPath;
        if (!Directory.Exists(path))
            throw new ConfigurationException($"Invalid configuration path ({mArgsInfo.ConfigurationPath})");

        mConfigurationFile = GetFile(mArgsInfo.Role);
    }

    /// <inheritdoc />
    public async Task<CellBuildConfiguration> Get(CancellationToken token = default)
    {
        DebugLogger.WriteLine("Start getting configuration...");

        var configJObject =
            JsonConvert.DeserializeObject<JObject>(await File.ReadAllTextAsync(mConfigurationFile, token));
        
        await ResolveIncludes(configJObject, Path.GetDirectoryName(mConfigurationFile), mArgsInfo.Configuration, token);

        var modules = GetModulesNames(configJObject);

        var configuration = new CellBuildConfiguration
        {
            Role = mArgsInfo.Role,
            Configuration = mArgsInfo.Configuration,
            Build = mArgsInfo.Build,
            Modules = modules,
            JsonPayload = configJObject?.ToString(Formatting.None) ?? string.Empty
        };
        
        DebugLogger.WriteLine("Configuration is created", ConsoleColor.Green);

        return configuration;
    }

    private static string[] GetModulesNames(JObject config)
    {
        
        if (config == null) 
            throw new ConfigurationException("Configuration object is null");

        var modules = config[MODULES_KEY]?.Values<string>().ToArray() ?? Array.Empty<string>();
        if (modules.Length == 0)
            throw new ConfigurationException("At least one work module is required");
        config.Remove(MODULES_KEY);
        
        DebugLogger.WriteLine($"There are {modules.Length} modules names");
        return modules;
    }

    private async Task ResolveIncludes(JObject config, string importDirectory, string configuration, CancellationToken token = default)
    {
        DebugLogger.WriteLine("Resolving includes...");
        
        if (config == null) 
            return;
        
        foreach (var include in config[INCLUDES_KEY])
        {
            DebugLogger.WriteLine($"Include: {include}");

            var includeFile = include.Value<string>() ?? string.Empty;
            if (string.IsNullOrEmpty(includeFile))
            {
                DebugLogger.WriteLine($"Failed to get value of {include}", ConsoleColor.Yellow);
                continue;
            }

            includeFile = GetFile(includeFile);
            var jsonInclude = JsonConvert.DeserializeObject<JObject>(await File.ReadAllTextAsync(includeFile, token));
                
            if (jsonInclude == null) 
                continue;

            config.Merge(jsonInclude, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Union});
        }

        config.Remove(INCLUDES_KEY);
    }
    
    private string GetFile(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        
        // Search for <role>.<configuration>.json
        var file = GetConfigurationFilePath(mArgsInfo.ConfigurationPath.LocalPath, 
            $"{name}.{mArgsInfo.Configuration}");
        
        if(string.IsNullOrEmpty(file))
            // Search for <role>.json
            file = GetConfigurationFilePath(mArgsInfo.ConfigurationPath.LocalPath, name);
        
        if(string.IsNullOrEmpty(file))
            throw new ConfigurationException($"Configuration for role/import {name} was not found");

        return file;
    }
    
    private static string GetConfigurationFilePath(string path, string fileName)
    {
        var files = Directory.GetFiles(path, $"{fileName}.json", SearchOption.AllDirectories);
        if (files.Length > 1)
            throw new ConfigurationException($"There is more configuration files: {fileName}.json")
            {
                Data = { {"Files", files} }
            };

        return files.Length == 0 ? null : files[0];
    }
}