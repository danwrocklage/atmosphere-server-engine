namespace AUtils.IoC.Containers;

/// <inheritdoc/>
internal class StaticContainer : IContainer
{
    private bool mIsDisposed;
    private readonly IReadOnlyDictionary<Type, HashSet<IDependency>> mDependencyItems;

    internal StaticContainer(IReadOnlyDictionary<Type, HashSet<IDependency>> dependencyItems)
    {
        mDependencyItems = dependencyItems;
    }

    /// <inheritdoc/>
    public T Resolve<T>() => (T) Resolve(typeof(T));

    /// <inheritdoc/>
    public object Resolve(Type type)
    {
        var genericArgs = type.IsArray ? new []{ type.GetElementType() } : type.IsGenericType ? type.GetGenericArguments() : null;
        var isEnumerable = type.IsArray ||
                           (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            
        var dependencies = GetDependency(type, genericArgs, isEnumerable);
        if (!isEnumerable)
            return dependencies[0].Get(genericArgs);
            
        var result = new object[dependencies.Count];
        for (var i = 0; i < result.Length; i++)
            result[i] = dependencies[i].Get();

        var output = Array.CreateInstance(genericArgs![0], result.Length);
        Array.Copy(result, output, result.Length);

        return output;
    }

    private ArraySegment<IDependency> GetDependency(Type type, Type[] genericArgs, bool isEnumerable)
    {
        if (mIsDisposed)
            throw new ObjectDisposedException(nameof(IContainer));

        var targetType = isEnumerable ? genericArgs[0] : type;

        if (!mDependencyItems.TryGetValue(targetType, out var dependencies))
        {
            if (type.IsGenericType)
            {
                targetType = type.GetGenericTypeDefinition();
                if (!mDependencyItems.TryGetValue(targetType, out dependencies))
                    throw new ResolveException(targetType);
            }
            else
            {
                if (isEnumerable)
                    return ArraySegment<IDependency>.Empty;

                throw new ResolveException(type);
            }
        }

        return !isEnumerable
            ? new ArraySegment<IDependency>(dependencies.ToArray(), 0, 1)
            : new ArraySegment<IDependency>(dependencies.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        if (mIsDisposed)
            return;
        
        foreach (var dependencies in mDependencyItems.Values)
        {
            foreach (var dependency in dependencies)
            {
                if(dependency is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
            }
        }

        mIsDisposed = true;
    }
}