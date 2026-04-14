using System.Linq.Expressions;
using System.Reflection;

namespace AGame.Actors.Avatar;

internal readonly struct AvatarBaseOf<T>
{
    private const string TOPIC_PREFIX = $"{RpcTopics.ACTOR_RPC}.";
    
    private readonly string mRpcTopic;

    public AvatarBaseOf(Guid cellId, AvatarContext avatarContext)
    {
        mRpcTopic = string.Concat(TOPIC_PREFIX, cellId.ToString());
        CellId = cellId;
        AvatarContext = avatarContext;
    }

    public Guid CellId { get; }

    public AvatarContext AvatarContext { get; }
    
    #region Remote procedure call

    internal Task Rpc<TItem>(Expression<Func<T, TItem>> expression, Guid id, TItem value, (Type Type, string Name) component = default,
        CancellationToken token = default)
    {
        if (expression == null) 
            throw new ArgumentNullException(nameof(expression));
        
        if(expression.Body is not MemberExpression {Member.MemberType: MemberTypes.Property} memberExpression)
            throw new ArgumentException("Only property access are supported");
        
        if(memberExpression.Member is PropertyInfo {CanWrite:false})
            throw new ArgumentException("Only property with setter are supported");

        var command = new RpcRequest
        {
            Method = memberExpression.Member.Name,
            ActorId = id,
            Arguments = new object[] {value},
            Component = component != default ? 
                new RpcComponent {Name = component.Name, Type = component.Type.AssemblyQualifiedName} : 
                null
        };

        return AvatarContext.Rpc.Call<RpcRequest, RpcResponse>(mRpcTopic, command, token);
    }
    
    internal Task Rpc(Expression<Action<T>> expression, Guid id, (Type Type, string Name) component = default,
        CancellationToken token = default)
    {
        if (expression == null) 
            throw new ArgumentNullException(nameof(expression));
        
        var command = GetRequest(expression.Body, id);
        if (component != default)
            command.Component = new RpcComponent {Name = component.Name, Type = component.Type.AssemblyQualifiedName};
        return AvatarContext.Rpc.Call<RpcRequest, RpcResponse>(mRpcTopic, command, token);
    }

    internal Task Rpc(Expression<Func<T, Task>> expression, Guid id, (Type Type, string Name) component = default,
        CancellationToken token = default)
    {
        if (expression == null) 
            throw new ArgumentNullException(nameof(expression));
        
        var command = GetRequest(expression.Body, id);
        if (component != default)
            command.Component = new RpcComponent {Name = component.Name, Type = component.Type.AssemblyQualifiedName};
        return AvatarContext.Rpc.Call<RpcRequest, RpcResponse>(mRpcTopic, command, token);
    }

    internal async Task<TItem> Rpc<TItem>(Expression<Func<T, TItem>> expression, Guid id,
        (Type Type, string Name) component = default, CancellationToken token = default)
    {
        if (expression == null) 
            throw new ArgumentNullException(nameof(expression));
        
        var command = GetRequest(expression.Body, id);
        if (component != default)
            command.Component = new RpcComponent {Name = component.Name, Type = component.Type.AssemblyQualifiedName};
        var response = await AvatarContext.Rpc.Call<RpcRequest, RpcResponse>(mRpcTopic, command, token);
        return response is {IsSuccess: true, Reply: TItem item} ? item : default;
    }

    internal async Task<TItem> Rpc<TItem>(Expression<Func<T, Task<TItem>>> expression, Guid id,
        (Type Type, string Name) component = default, CancellationToken token = default)
    {
        if (expression == null) 
            throw new ArgumentNullException(nameof(expression));
        
        var command = GetRequest(expression.Body, id);
        if (component != default)
            command.Component = new RpcComponent {Name = component.Name, Type = component.Type.AssemblyQualifiedName};
        var response = await AvatarContext.Rpc.Call<RpcRequest, RpcResponse>(mRpcTopic, command, token);
        return response is {IsSuccess: true, Reply: TItem item} ? item : default;
    }

    private RpcRequest GetRequest(Expression body, Guid actorId)
    {
        switch (body)
        {
            case MethodCallExpression methodCall:
            {
                var args = new object[methodCall.Arguments.Count];
                for (var i = 0; i < methodCall.Arguments.Count; i++)
                {
                    var arg = methodCall.Arguments[i];
                    args[i] = arg is ConstantExpression constArgument
                        ? constArgument.Value
                        : Expression.Lambda(arg).Compile().DynamicInvoke();
                }

                return new RpcRequest
                {
                    ActorId = actorId,
                    Arguments = args.ToArray(),
                    Method = methodCall.Method.Name
                };
            }
            case MemberExpression {Member.MemberType: MemberTypes.Property} memberExpression:
                return new RpcRequest
                {
                    ActorId = actorId,
                    Arguments = null,
                    Method = memberExpression.Member.Name
                };
            default:
                throw new ArgumentException("Only method call and property access are supported");
        }
    }

    #endregion
}