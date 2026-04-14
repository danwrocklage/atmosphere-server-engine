using System.Text.Json.Serialization;

namespace ACore.Logging;

internal class LogstashEvent
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
        
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("exception-stacktrace")]
    public string ExceptionStacktrace { get; set; }
        
    [JsonPropertyName("exception-message")]
    public string ExceptionMessage { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; }

    [JsonPropertyName("cell-name")]
    public string Role { get; set; }

    [JsonPropertyName("version")]
    public string Build { get; set; }
        
    [JsonPropertyName("configuration")]
    public string Configuration { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; }
}