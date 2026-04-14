namespace AUtils.IoC.Dependencies;

internal class StaticDependency : DependencyBase, IDependency
{
    private readonly Func<object> mInstanceFunc;

    public StaticDependency(Type type, Func<object> instanceFunc) : base(type)
    {
        mInstanceFunc = instanceFunc;
    }
        
    /// <summary>
    /// Resolving type
    /// </summary>
    public Type GetDependencyType() => Type;

    /// <summary>
    /// Create resolving object
    /// </summary>
    /// <param name="genericArgs"></param>
    public object Get(Type[] genericArgs = null) => mInstanceFunc();
}