using ACore.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACore.Configuration.Providers;

internal class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly JObject mConfigurations;

    public JsonConfigurationProvider(string json)
    {
        mConfigurations = JsonConvert.DeserializeObject<JObject>(json) ?? new JObject();
    }

    public bool IsExists(string key) => mConfigurations.ContainsKey(key);

    public (T Value, bool IsValueGot) Get<T>(string key) => 
        !mConfigurations.TryGetValue(key, StringComparison.InvariantCultureIgnoreCase, out var token) ? 
            (default, false) : 
            (token.ToObject<T>(), true);
}