using System.Diagnostics;
using AUtils.IoC.Containers;

namespace AUtils.IoC.Dependencies;

[DebuggerDisplay("{typeof(T).Name} container dependency")]
internal class ContainerDependency : DependencyBase, IDependency
{
    private readonly Func<IContainer, Type, object> mResolver;
    private readonly IContainer mContainer;

    public ContainerDependency(Type type, Func<IContainer, Type, object> resolver, IReadOnlyCollection<DependencyBuilder> dependencyBuilders)
        : base(type)
    {
        mResolver = resolver;
        mContainer = new RuntimeContainer(dependencyBuilders);
    }

    public Type GetDependencyType() => Type;

    public object Get(Type[] genericArgs = null) => 
        mResolver(mContainer, Type.IsGenericType ? Type.MakeGenericType(genericArgs) : Type);
}