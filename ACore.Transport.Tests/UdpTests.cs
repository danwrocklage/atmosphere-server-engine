using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACore.Tests.Shared;
using ACore.Transport.Udp;
using AUtils.IoC;
using Xunit;

namespace ACore.Transport.Tests;

public class UdpTests
{
    [Fact]
    public void TransportModuleBuildContainerTest()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        new TransportModule().ConfigureServices(builder);
        var factory = builder.Build().Resolve<TransportFactory>();
        Assert.NotNull(factory);
    }
    
    [Fact]
    public async Task StartUdpServerTest()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        new TransportModule().ConfigureServices(builder);
        var factory = builder.Build().Resolve<TransportFactory>();
        await using var server = factory.CreateServer(TransportType.UDP);
        Assert.NotNull(server);
        Assert.IsType<UdpServer>(server);
        
        ((UdpServer)server).Initialize();
        
        var cts = new CancellationTokenSource(200);
        await server.Run(cts.Token);
    }
    
    [Fact]
    public void PrepareUdpClientTest()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        new TransportModule().ConfigureServices(builder);
        var factory = builder.Build().Resolve<TransportFactory>();
        using var client = factory.CreateClient(TransportType.UDP, "test_client");
        Assert.NotNull(client);
        Assert.IsType<UdpClient>(client);
    }
    
    [Fact(Timeout = 5_000)]
    public async Task ListenClientServerTest()
    {
        var builder = new ContainerBuilder();
        builder.AddFakeServices();
        new TransportModule().ConfigureServices(builder);
        var factory = builder.Build().Resolve<TransportFactory>();
        using var client = factory.CreateClient(TransportType.UDP, "test_client");
        var cts = new CancellationTokenSource();
        var isConnected = false;

        await using var server = (UdpServer) factory.CreateServer(TransportType.UDP);
        server.NewConnection += async (connection, token) =>
        {
            Assert.NotNull(connection);
            Assert.False(token.IsCancellationRequested);
            Assert.IsType<UdpServerConnection>(connection);

            using var message = await connection.Receive(token);

            Assert.Equal("SomeString", Encoding.UTF8.GetString(message.Data.Span));
            isConnected = true;
            await connection.Send((Memory<byte>)Encoding.UTF8.GetBytes("ServerBackString"), token);
        };
        server.Initialize();
        server.RunNonBlocking(cts.Token);

        await client.Connect("127.0.0.1", 4000);
        await client.Send((Memory<byte>)Encoding.UTF8.GetBytes("SomeString"));


        await Task.Delay(500);
        Assert.True(isConnected);

        using var packet = await client.Receive();

        Assert.Equal("ServerBackString", Encoding.UTF8.GetString(packet.Data.Span));
        
        cts.Cancel();
        client.Disconnect();
    }
}