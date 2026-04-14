using ACore.Abstractions;

namespace ACore.Transport;

[Configuration("transport.server")]
internal class TransportServerConfig
{
    public int InPort { get; set; }
            
    public int Timeout { get; set; }
            
    public int BufferSize { get; set; }

    public static TransportServerConfig Default => new()
    {
        InPort = 4000,
        Timeout = 5 * 1000,
        BufferSize = 2048 + 512
    };
}