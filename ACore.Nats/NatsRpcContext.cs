using System.Buffers;
using ACore.Abstractions.Rpc;
using AUtils.Sil;
using NATS.Client;

namespace ACore.Nats;

internal class NatsRpcContext<T> : IRpcContext<T>
{
    private readonly IConnection mConnection;
    private readonly string mReplyTopic;

    public NatsRpcContext(T message, string sender, string replyTopic, IConnection connection)
    {
        Message = message;
        Sender = sender;
        mReplyTopic = replyTopic;
        mConnection = connection;
    }

    public T Message { get; }
    public string Sender { get; }
    public bool IsReplyRequired => !string.IsNullOrEmpty(mReplyTopic);

    internal bool WasReplied { get; private set; }

    public void Reply<TReply>(TReply message)
    {
        if (!IsReplyRequired)
            return;

        var buffer = new byte[Sil.OutputSize(message)];
        Sil.Serialize(message, buffer);
        mConnection.Publish(mReplyTopic, buffer);
        mConnection.Flush();

        WasReplied = true;
    }
}