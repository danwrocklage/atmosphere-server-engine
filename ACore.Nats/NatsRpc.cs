using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Telemetry;
using AUtils.IoC;
using AUtils.Sil;
using NATS.Client;

namespace ACore.Nats;

[Log(Category = "[rpc] nats")]
internal class NatsRpc : IRpc, IInitializable, IDisposable, IRpcSubscribe
{
    private static readonly ConcurrentDictionary<Type, RpcType> sRpcSubscriptionTypes = new();
    private const string SENDER_HEADER = "sender";

    private readonly IContainer mContainer;
    private readonly ICellMetrics mMetrics;
    private readonly string mRoleName;
    private readonly ILogger<NatsRpc> mLogger;
    private readonly Dictionary<string, List<IAsyncSubscription>> mSubscriptions;
    private IConnection mConnection;
    private int mRequestTimeout;
    private bool mIsEnabled;
    private string mTopicPrefix;

    public NatsRpc(ILogger<NatsRpc> logger, ICellEnvironment environment, 
        IContainer container, ICellMetrics metrics)
    {
        mLogger = logger;
        mContainer = container;
        mMetrics = metrics;
        mRoleName = environment.Role;
        mSubscriptions = new Dictionary<string, List<IAsyncSubscription>>();
    }

    public void Initialize()
    {
        var config = mContainer.Resolve<IConfiguration>()
            .Get(() => NatsConfiguration.Default);
        var connectionFactory = new ConnectionFactory();
        var options = ConnectionFactory.GetDefaultOptions();
        options.Servers = config.ConnectionString.Split(',');
        options.Timeout = config.Timeout * 1000;
        options.AllowReconnect = true;
        options.Name = mRoleName;
        options.MaxReconnect = config.Reconnects;
        options.ReconnectWait = config.Timeout * 1000;

        mRequestTimeout = config.RequestTimeout * 1000;
        mTopicPrefix = config.TopicPrefix; 

        options.AsyncErrorEventHandler = (_, args) =>
            mLogger.Error($"Error {args.Error}, topic: {args.Subscription.Subject}");
        options.ClosedEventHandler = (_, _) =>
        {
            mLogger.Info("Connection was closed");
            mIsEnabled = false;
        };
        options.DisconnectedEventHandler = (_, _) =>
        {
            mLogger.Warn("Connection was disconnected");
            mIsEnabled = false;
        };
        options.ReconnectedEventHandler = (_, _) =>
        {
            mLogger.Info("Connection was restored");
            mIsEnabled = true;
        };
        options.ServerDiscoveredEventHandler = (_, _) =>
            mLogger.Info("A new server was found in cluster");

        try
        {
            mConnection = connectionFactory.CreateConnection(options);
            mIsEnabled = mConnection.State == ConnState.CONNECTED;

            if (mIsEnabled)
                mLogger.Success($"Connected to [{config.ConnectionString}]");
        }
        catch (NATSNoServersException e)
        {
            mLogger.Error($"There is no Nats server(s) {config.ConnectionString}", e);
            mIsEnabled = false;
        }
        catch (NATSConnectionException e)
        {
            mLogger.Error($"Can't connect to Nats server(s) {config.ConnectionString}", e);
            mIsEnabled = false;
        }
        catch (Exception e)
        {
            mLogger.Error($"Something gets wrong with Nats server(s) {config.ConnectionString}", e);
            mIsEnabled = false;
        }

        CreateMetrics();
    }

    #region Publish

    public Task<TReply> Call<TRequest, TReply>(TRequest request, CancellationToken token = default)
    {
        var topic = GetTopic<TRequest>();
        return Call<TRequest, TReply>(topic, request, token);
    }

    public async Task<TReply> Call<TRequest, TReply>(string topic, TRequest request, CancellationToken token = default)
    {
        if (!mIsEnabled)
        {
            mLogger.Debug($"[Disabled] Fire {request.GetType().Name} to {topic}");
            return default;
        }

        var type = GetMessageType<TRequest>();
        if (type != RpcType.Request)
            throw new CellException($"{typeof(TRequest).FullName} is not request (Type: {type})");

        topic = ApplyPrefix(topic);

        mMetrics.Get("rpc_request_count").Inc(topic, mRoleName);

        var msg = new Msg
        {
            Subject = topic,
            Header = {[SENDER_HEADER] = mRoleName}
        };
        var buffer = ArrayPool<byte>.Shared.Rent(Sil.OutputSize(request));
        Sil.Serialize(request, buffer);
        msg.AssignData(buffer);

        try
        {
            var reply = await mConnection.RequestAsync(msg, mRequestTimeout, token);
            return (TReply) Sil.Deserialize(reply.Data).Result;
        }
        catch (NATSNoRespondersException)
        {
            mLogger.Debug($"There is no responders for request ({topic})");
            mMetrics.Get("rpc_request_error_count").Inc(topic, mRoleName);
            return default;
        }
        catch (NATSTimeoutException)
        {
            mLogger.Debug($"The rpc request was cancelled by timeout ({topic})");
            mMetrics.Get("rpc_request_error_count").Inc(topic, mRoleName);
            return default;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task Call<TRequest>(TRequest request, CancellationToken token = default)
    {
        var topic = GetTopic<TRequest>();
        return Call(topic, request, token);
    }

    public Task Call<TRequest>(string topic, TRequest request, CancellationToken token = default)
    {
        if (!mIsEnabled)
        {
            mLogger.Debug($"[Disabled] Fire {request.GetType().Name} to {topic}");
            return default;
        }
        
        var type = GetMessageType<TRequest>();
        if (type == RpcType.Request)
            throw new CellException($"{typeof(TRequest).FullName} is request, but use as non request (Type: {type})");
        
        topic = ApplyPrefix(topic);

        var msg = new Msg
        {
            Subject = topic,
            Header = {[SENDER_HEADER] = mRoleName}
        };
        var buffer = ArrayPool<byte>.Shared.Rent(Sil.OutputSize(request));
        Sil.Serialize(request, buffer);
        msg.AssignData(buffer);
        mConnection.Publish(msg);
        ArrayPool<byte>.Shared.Return(buffer);
        mMetrics.Get("rpc_publish_count").Inc(topic, mRoleName);

        return Task.CompletedTask;
    }

    #endregion

    #region Subscription

    public void Subscribe<T>() => Subscribe(mContainer.Resolve<IRpcHandler<T>>());

    public void Subscribe<T>(params string[] topics)
    {
        var handler = mContainer.Resolve<IRpcHandler<T>>();
        foreach (var topic in topics)
            Subscribe(topic, handler);
    }

    public void Subscribe<T>(IRpcHandler<T> handler)
    {
        var topic = GetTopic<T>();
        Subscribe(topic, handler);
    }
    
    public void Subscribe<T>(string topic, IRpcHandler<T> handler)
    {
        var type = GetMessageType<T>();

        if (!mIsEnabled)
        {
            mLogger.Debug($"[Disabled] Subscribe to {topic} (Type: {type})");
            return;
        }

        topic = ApplyPrefix(topic);
        
        var subscription = type == RpcType.Fanout ? 
            mConnection.SubscribeAsync(topic, (_, args) => MessageHandle(handler, args.Message)) :
            mConnection.SubscribeAsync(topic, mRoleName, (_, args) => MessageHandle(handler, args.Message));

        mLogger.Debug($"Subscribed on \"{topic}\" (Type: {type})");
        
        if (!mSubscriptions.TryGetValue(topic, out var subscriptions))
        {
            var topicSubscriptions = new List<IAsyncSubscription> {subscription};
            mSubscriptions.Add(topic, topicSubscriptions);
        }

        subscriptions?.Add(subscription);
    }

    private void MessageHandle<T>(IRpcHandler<T> handler, Msg msg)
    {
        mMetrics.Get("rpc_received_count").Inc(msg.Subject, mRoleName);
        
        var sender = msg.Header[SENDER_HEADER] ?? "Unknown";
        //mLogger.Debug($"Receive message in \"{msg.Subject}\" from {sender}");
        var (message, messageType) = Sil.Deserialize(msg.Data);
        if (!messageType.IsAssignableTo(typeof(T)))
        {
            mLogger.Warn($"Dead message (wrong type). Expect {typeof(T).FullName}, got {messageType.FullName} on \"{msg.Subject}\"");
            return;
        }

        var context = new NatsRpcContext<T>((T) message, sender, msg.Reply, mConnection);
        handler.Handle(context)
            .ContinueWith(t => { mLogger.Error($"Failed with message handle on \"{msg.Subject}\"", t.Exception); }, TaskContinuationOptions.OnlyOnFaulted)
            .ContinueWith(_ =>
            {
                if (context.IsReplyRequired && !context.WasReplied) 
                    mLogger.Error($"Request wasn't replied ({msg.Subject})");
            })
            .ConfigureAwait(false);
    }
    
    #endregion

    private void CreateMetrics()
    {
        mMetrics.Create("rpc_publish_count", MetricsType.Counter, labels: new [] {"topic", "role"});
        mMetrics.Create("rpc_received_count", MetricsType.Counter, labels: new [] {"topic", "role"});
        mMetrics.Create("rpc_request_count", MetricsType.Counter, labels: new [] {"topic", "role"});
        mMetrics.Create("rpc_request_error_count", MetricsType.Counter, labels: new [] {"topic", "role"});
    }

    private string GetTopic<T>() =>
        typeof(T).GetCustomAttribute<TopicAttribute>()?.Topic ?? 
        typeof(T).FullName ?? 
        throw new CellException();
    
    private RpcType GetMessageType<TRequest>()
    {
        if (sRpcSubscriptionTypes.TryGetValue(typeof(TRequest), out var type))
            return type;

        type = typeof(TRequest).GetCustomAttribute<TopicAttribute>()?.Type ?? RpcType.Group;
        sRpcSubscriptionTypes.TryAdd(typeof(TRequest), type);

        return type;
    }
    
    private string ApplyPrefix(string topic)
    {
        if (topic == null) 
            throw new ArgumentNullException(nameof(topic));

        return $"{mTopicPrefix}.{topic}";
    }
    
    public void Dispose()
    {
        foreach (var subscription in mSubscriptions
                     .SelectMany(subscriptions => subscriptions.Value))
            subscription.Unsubscribe();
    }

    #region Utils

    [Configuration("mb.nats")]
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Local")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    private class NatsConfiguration
    {
        public string ConnectionString { get; set; }

        public int Timeout { get; set; }

        public int Reconnects { get; set; }
        
        public int RequestTimeout { get; set; }
        
        public string TopicPrefix { get; set; }

        public static NatsConfiguration Default => new()
        {
            ConnectionString = "localhost:4222",
            TopicPrefix = "acore",
            Reconnects = 2,
            Timeout = 3,
            RequestTimeout = 1
        };
    }

    #endregion
}