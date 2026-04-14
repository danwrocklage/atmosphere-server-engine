using ACore.Abstractions;

namespace ACore.Worker.Web;

/// <summary>
/// Web api worker configuration
/// </summary>
[Configuration("http.web")]
internal class WebWorkerConfig
{
    /// <summary>
    /// Listening path
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Listening port
    /// </summary>
    public int PortOut { get; set; }

    public static WebWorkerConfig Default => new()
    {
        Path = "api",
        PortOut = 5000
    };
}