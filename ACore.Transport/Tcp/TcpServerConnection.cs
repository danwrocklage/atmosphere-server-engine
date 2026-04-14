using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using ACore.Abstractions.Transport;

namespace ACore.Transport.Tcp;

internal class TcpServerConnection : IConnection
{
    private readonly NetworkStream mNetworkStream;
    private readonly System.Net.Sockets.TcpClient mTcpClient;
    private readonly Memory<byte> mInputSizeBuffer;
    private readonly Memory<byte> mOutputSizeBuffer;

    public TcpServerConnection(System.Net.Sockets.TcpClient tcpClient)
    {
        mTcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        mNetworkStream = tcpClient.GetStream();
        RemoteEndpoint = tcpClient.Client.RemoteEndPoint;
        mInputSizeBuffer = new Memory<byte>(new byte[4]);
        mOutputSizeBuffer = new Memory<byte>(new byte[4]);
    }
    
    public EndPoint RemoteEndpoint { get; }

    public async Task Send(Packet packet, CancellationToken token = default)
    {
        if(packet.Data.IsEmpty)
            return;

        token.ThrowIfCancellationRequested();
        
        Unsafe.As<byte, int>(ref mInputSizeBuffer.Span[0]) = packet.Data.Length;
        await mNetworkStream.WriteAsync(mInputSizeBuffer, token);
        await mNetworkStream.WriteAsync(packet.Data, token);
    }

    public async Task<Packet> Receive(CancellationToken token = default)
    {
        var read = await mNetworkStream.ReadAsync(mOutputSizeBuffer, token);
        if (read != 4)
            return Packet.Empty;

        var bufferLen = BitConverter.ToInt32(mOutputSizeBuffer.Span);

        var buffer = MemoryPool<byte>.Shared.Rent(bufferLen);
        read = await mNetworkStream.ReadAsync(buffer.Memory, token);
        
        return new Packet(buffer).Slice(0, read);
    }
    
    public void Disconnect()
    {
        mTcpClient.Close();
    }
}