using System.Runtime.CompilerServices;
using ACore.Transport.Tcp;
using ACore.Transport.Udp;
using AUtils.IoC;

[assembly:InternalsVisibleTo("ACore.Transport.Tests")]

namespace ACore.Transport;

public class TransportModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Transient<TransportFactory>();

        builder.Transient<UdpServer>();
        builder.Transient<UdpClient>();
        
        builder.Transient<TcpServer>();
        builder.Transient<TcpClient>();
    }
}