using ACore.Abstractions;

namespace ACore.Application.Commands;

[Configuration("http.control")]
internal class HttpControlConfig
{
    public int PortOut { get; set; }
    
    public string MetricsEndpoint { get; set; }

    public static HttpControlConfig Default => new()
    {
        PortOut = 6001,
        MetricsEndpoint = "metrics"
    };
}