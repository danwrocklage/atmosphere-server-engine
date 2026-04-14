namespace AUtils.IoC;

/// <summary>
/// Dependency container
/// </summary>
public interface IContainer : IAsyncDisposable
{
    /// <summary>
    /// Resolve dependency of <see cref="T"/>
    /// </summary>
    T Resolve<T>();

    /// <summary>
    /// Resolve dependency of <see cref="Type"/>
    /// </summary>
    object Resolve(Type type);
}