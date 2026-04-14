using System.Diagnostics;

namespace AUtils.IoC.Dependencies;

/// <summary>
/// Start time dependency
/// </summary>
[DebuggerDisplay("{typeof(T).Name} singleton dependency")]
internal class SingletonDependency : DependencyBase, IDependency, IAsyncDisposable
{
    private object mInstance;

    internal SingletonDependency(
        Type type,
        IReadOnlyCollection<DependencyBuilder> dependencyBuilders, 
        IReadOnlyCollection<IDependency> staticDependencies) :base(type)
    {
        GetDependencies(dependencyBuilders, staticDependencies);
        mInstance = CreateInstance(null);
    }

    internal SingletonDependency(object instance) : base(instance?.GetType())
    {
        mInstance = instance ?? throw new ArgumentNullException(nameof(instance));
    }

    /// <inheritdoc/>
    public Type GetDependencyType() => Type;

    /// <param name="genericArgs"></param>
    /// <inheritdoc/>
    public object Get(Type[] genericArgs = null) => mInstance;
        
    public async ValueTask DisposeAsync()
    {
        switch (mInstance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable dis:
                dis.Dispose();
                break;
        }

        mInstance = default;
    }
}