namespace AUtils.IoC;

/// <summary>
/// Object resolving dependency
/// </summary>
internal interface IDependency
{
    /// <summary>
    /// Resolving type
    /// </summary>
    Type GetDependencyType();

    /// <summary>
    /// Create resolving object
    /// </summary>
    /// <param name="genericArgs"></param>
    object Get(Type[] genericArgs = null);
}