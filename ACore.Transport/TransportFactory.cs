using System.Diagnostics.CodeAnalysis;
using ACore.Abstractions.Transport;
using ACore.Transport.Tcp;
using ACore.Transport.Udp;
using AUtils.IoC;

namespace ACore.Transport;

/// <summary>
/// Connection type
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum TransportType
{
    /// <summary>
    /// TCP transport
    /// </summary>
    TCP,
        
    /// <summary>
    /// UDP transport
    /// </summary>
    UDP
}
    
/// <summary>
/// Factory for creating clients and listening servers
/// </summary>
public sealed class TransportFactory
{
    private readonly IContainer mContainer;

    public TransportFactory(IContainer container)
    {
        mContainer = container;
    }

    /// <summary>
    /// Create listener server with specified <see cref="TransportType"/>
    /// </summary>
    public IServer CreateServer(TransportType type) =>
        type switch
        {
            TransportType.TCP => mContainer.Resolve<TcpServer>(),
            TransportType.UDP => mContainer.Resolve<UdpServer>(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
    /// <summary>
    /// Create named client with specified <see cref="TransportType"/>
    /// </summary>
    public IClient CreateClient(TransportType type, string name)
    {
        switch (type)
        {
            case TransportType.UDP:
                var udpClient = mContainer.Resolve<UdpClient>();
                udpClient.ClientName = name;
                return udpClient;
            case TransportType.TCP: 
                var tcpClient = mContainer.Resolve<TcpClient>();
                tcpClient.ClientName = name;
                return tcpClient;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}