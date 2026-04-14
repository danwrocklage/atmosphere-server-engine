namespace AUtils.IoC.Containers;

internal class RuntimeContainer : IContainer
{
    private readonly IReadOnlyCollection<DependencyBuilder> mDependencyBuilders;

    public RuntimeContainer(IReadOnlyCollection<DependencyBuilder> dependencyBuilders)
    {
        mDependencyBuilders = dependencyBuilders;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public T1 Resolve<T1>() => (T1) Resolve(typeof(T1));

    public object Resolve(Type type)
    {
        var genericArgs = type.IsArray ? new[] {type.GetElementType()} :
            type.IsGenericType ? type.GetGenericArguments() : null;

        var dependencyBuilder = mDependencyBuilders.FirstOrDefault(x => x.AsTypes.Contains(type));
        if (dependencyBuilder == null)
        {
            if (!type.IsGenericType)
                throw new ResolveException(typeof(object), type);
            
            dependencyBuilder = mDependencyBuilders.FirstOrDefault(x => x.AsTypes.Contains(type.GetGenericTypeDefinition()));
            if (dependencyBuilder == null)
                throw new ResolveException(typeof(object), type.GetGenericTypeDefinition());
        }

        var dependency = dependencyBuilder.Build();
        return dependency.Get(genericArgs);
    }
}