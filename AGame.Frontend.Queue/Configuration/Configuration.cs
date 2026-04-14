using ACore.Abstractions;

namespace AGame.Frontend.Queue;

[Configuration("connections")]
internal class Configuration
{
    public int MaxConnections { get; set; }
    
    public int PrepareSeconds { get; set; }
    
    public TimeSpan PrepareTime => TimeSpan.FromSeconds(PrepareSeconds);

    public static Configuration Default => new()
    {
        MaxConnections = 1000,
        PrepareSeconds = 10
    };
}