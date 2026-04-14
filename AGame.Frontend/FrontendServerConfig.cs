using System.Diagnostics.CodeAnalysis;
using ACore.Abstractions;
using ACore.Transport;

namespace AGame.Frontend;

[Configuration("front.server")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
internal class FrontendServerConfig
{
    public string Protocol { get; set; }

    public bool Encryption { get; set; }

    public bool Compression { get; set; }

    public int PrepareSeconds { get; set; }

    public TimeSpan PrepareTime => TimeSpan.FromSeconds(PrepareSeconds);

    public TransportType TransportType => Protocol switch
    {
        "udp" => TransportType.UDP,
        "tcp" => TransportType.TCP,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static FrontendServerConfig Default => new()
    {
        Protocol = "udp",
        Compression = true,
        Encryption = true,
        PrepareSeconds = 10
    };
}