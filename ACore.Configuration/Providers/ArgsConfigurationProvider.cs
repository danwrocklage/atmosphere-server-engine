using System.Text.Json;
using ACore.Abstractions;
using ACore.Application;

namespace ACore.Configuration.Providers;

internal class ArgsConfigurationProvider : IConfigurationProvider
{
    private const char PARAMETER_DELIMITER = '=';

    private readonly Dictionary<string, string> mArgs;
    
    public ArgsConfigurationProvider()
    {
        var userArgs = Environment.GetCommandLineArgs()
            .SkipWhile(x => x != "::")
            .Skip(1).ToArray();

        if(userArgs.Any())
            DebugLogger.WriteLine($"User defined args: {string.Join(',', userArgs)}");
        
        mArgs = new Dictionary<string, string>();

        foreach (var arg in userArgs)
        {
            var keyAndValue = arg.Split(PARAMETER_DELIMITER);
            if(keyAndValue.Length != 2)
                continue;
                
            mArgs.Add(keyAndValue[0], keyAndValue[1]);
        }
    }
    
    public bool IsExists(string key) => mArgs.ContainsKey(key);

    public (T, bool) Get<T>(string key)
    {
        if (!mArgs.TryGetValue(key, out var value))
            return (default, false);
        
        return (typeof(T).Namespace == "System" ? 
            (T) Convert.ChangeType(value, typeof(T)) : 
            JsonSerializer.Deserialize<T>(value), true);
    }
}