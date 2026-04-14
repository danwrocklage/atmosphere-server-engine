using System.Collections.Concurrent;
using System.Reflection;
using ACore.Abstractions.Rpc;
using AGame.Actors.Avatar;
using AUtils.MethodExec;

namespace AGame.Actors.Handlers;

internal class RpcActorHandler : IRpcHandler<RpcRequest>
{
    private static readonly HashSet<string> sActorMethods = typeof(object)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Concat(typeof(Actor).GetMethods(BindingFlags.Instance|BindingFlags.Public))
        .Concat(typeof(ActorComponent).GetMethods(BindingFlags.Instance|BindingFlags.Public))
        .Select(x => x.Name)
        .Distinct()
        .ToHashSet();
    
    private static readonly HashSet<string> sComponentMethods = typeof(object)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Concat(typeof(ActorComponent).GetMethods(BindingFlags.Instance|BindingFlags.Public))
        .Select(x => x.Name)
        .Distinct()
        .ToHashSet();
        
    private static readonly ConcurrentDictionary<Type, Dictionary<string, ObjectMethodExecutor>> sTypesMethods = new();
    
    private readonly ActorContainer mActorContainer;

    public RpcActorHandler(ActorContainer actorContainer)
    {
        mActorContainer = actorContainer;
    }

    public async Task Handle(IRpcContext<RpcRequest> context, CancellationToken token = default)
    {
        var actor = mActorContainer.GetActor(context.Message.ActorId);

        var type = context.Message.Component != null ? Type.GetType(context.Message.Component.Type) : actor?.GetType();
        if (type == null)
        {
            context.Reply(new RpcResponse {IsSuccess = false});
            return;
        }

        object executable = context.Message.Component != null ? actor.Get(type, context.Message.Component.Name) : actor;
        
        if (!sTypesMethods.TryGetValue(type, out var methods))
        {
            IEnumerable<MethodInfo> rawMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            rawMethods = context.Message.Component != null ? 
                rawMethods.Where(x => !sComponentMethods.Contains(x.Name)) : 
                rawMethods.Where(x => !sActorMethods.Contains(x.Name));
            
            methods = rawMethods
                .ToDictionary(x => x.Name, x => ObjectMethodExecutor.Create(x, x.DeclaringType?.GetTypeInfo() ?? throw new Exception()));
            sTypesMethods.TryAdd(type, methods);
        }

        var name = context.Message.Arguments == null ? 
            $"get_{context.Message.Method}" : 
            context.Message.Method;

        if (!methods.TryGetValue(name, out var executor) &&
            context.Message.Arguments?.Length == 1)
            name = $"set_{context.Message.Method}";

        if (executor == null && 
            !methods.TryGetValue(name, out executor))
        {
            context.Reply(new RpcResponse {IsSuccess = false});
            return;
        }
        
        var result = executor.IsMethodAsync ? 
            await executor.ExecuteAsync(executable, context.Message.Arguments) : 
            executor.Execute(executable, context.Message.Arguments);
        
        context.Reply(new RpcResponse {Reply = result, IsSuccess = true});
    }
}