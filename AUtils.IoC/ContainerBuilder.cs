using AUtils.IoC.Containers;

namespace AUtils.IoC;

/// <summary>
/// Dependency container builder
/// </summary>
public class ContainerBuilder
{
    private readonly Dictionary<Type, HashSet<IDependency>> mDependencies = new();

    /// <summary>
    /// Dependencies builders list
    /// </summary>
    internal readonly List<DependencyBuilder> DependencyItems = new();

    /// <summary>
    /// When container has been built
    /// </summary>
    public event Action<IContainer> OnBuilt;

    /// <summary>
    /// Add new dependency container
    /// </summary>
    public ContainerBuilder Register(Action<DependencyBuilder> builder)
    {
        var db = new DependencyBuilder(this);
        builder(db);
        DependencyItems.Add(db);
        return this;
    }

    /// <summary>
    /// Check: can type <see cref="T"/> be resolved from building container
    /// </summary>
    public bool IsRegistered<T>() => IsRegistered(typeof(T));

    /// <summary>
    /// Check: can <paramref name="type"/> be resolved from building container
    /// </summary>
    public bool IsRegistered(Type type) => DependencyItems.Any(x => x.AsTypes.Contains(type));

    /// <summary>
    /// Build dependency container
    /// </summary>
    public IContainer Build()
    {
        if(!DependencyItems.Any(x => x.AsTypes.Contains(typeof(IContainer))))
        {
            var containerDependency = new DependencyBuilder(this)
                .For<RuntimeContainer>()
                .As<IContainer>()
                .Add<IReadOnlyCollection<DependencyBuilder>>(() => DependencyItems);
            DependencyItems.Add(containerDependency);
        }

        var resolveExceptions = new List<ResolveException>();
        foreach (var dependencyBuilder in DependencyItems)
        {
            try
            {
                var build = dependencyBuilder.Build();
                foreach (var at in dependencyBuilder.AsTypes)
                {
                    if (!mDependencies.ContainsKey(at))
                        mDependencies.Add(at, new HashSet<IDependency>());

                    if (!mDependencies[at].Contains(build))
                        mDependencies[at].Add(build);
                }
            }
            catch (Exception e)
            {
                if (e is ResolveException re)
                    resolveExceptions.Add(re);
                else
                    throw e.InnerException ?? e;
            }
        }

        if (resolveExceptions.Count > 0)
            throw new AggregateException($"{resolveExceptions.Count} items can't be resolved", resolveExceptions);
            
        var container = new StaticContainer(mDependencies);
        OnBuilt?.Invoke(container);
        return container;
    }
}