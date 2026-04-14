using System.Diagnostics;

namespace AUtils.IoC.Dependencies;

/// <summary>
/// Runtime resolving dependency
/// </summary>
[DebuggerDisplay("{typeof(T).Name} dynamic dependency")]
internal class DynamicDependency : DependencyBase, IDependency
{
    internal DynamicDependency(Type type, IReadOnlyCollection<DependencyBuilder> dependencies, IReadOnlyCollection<IDependency> staticDependencies) 
        : base(type)
    {
        GetDependencies(dependencies, staticDependencies);
    }
        
    /// <inheritdoc/>
    public Type GetDependencyType() => Type;

    /// <inheritdoc/>
    public object Get(Type[] genericArgs = null) => CreateInstance(genericArgs);
}