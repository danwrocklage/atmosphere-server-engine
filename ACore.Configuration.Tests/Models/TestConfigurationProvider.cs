using System.Collections.Generic;
using ACore.Abstractions;

namespace ACore.Configuration.Tests;

internal class TestConfigurationProvider : IConfigurationProvider
{
    private readonly Dictionary<string, object> mConfigs;

    public TestConfigurationProvider(Dictionary<string, object> configs)
    {
        mConfigs = configs;
    }

    public bool IsExists(string key)
    {
        return mConfigs.ContainsKey(key);
    }

    public (T Value, bool IsValueGot) Get<T>(string key)
    {
        return mConfigs.TryGetValue(key, out var config) ? ((T) config, true) : (default, false);
    }
}