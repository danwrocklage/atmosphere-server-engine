using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACore.Abstractions;

namespace Fb.Frontend.Bot;

internal class JsonFileConfigurationProvider : IConfigurationProvider
{
    private static readonly JsonSerializerOptions sSerializerOptions = new()
    {
        Converters = {new JsonStringEnumConverter()},
        PropertyNameCaseInsensitive = true
    };
    
    private readonly JsonDocument? mJsonObject;

    public JsonFileConfigurationProvider(string file)
    {
        if (!File.Exists(file))
            throw new FileNotFoundException("Json configuration file was not found");

        mJsonObject = JsonSerializer.Deserialize<JsonDocument>(File.ReadAllText(file, Encoding.Unicode));
    }
    
    public bool IsExists(string key) => mJsonObject?.RootElement.TryGetProperty(key, out _) ?? false;

    public (T? Value, bool IsValueGot) Get<T>(string key) =>
        mJsonObject?.RootElement.TryGetProperty(key, out var value) ?? false
            ? (value.Deserialize<T>(sSerializerOptions), true)
            : (default, false);
}