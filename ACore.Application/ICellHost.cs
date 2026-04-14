using ACore.Abstractions;
using AUtils.IoC;

namespace ACore.Application;

/// <summary>
/// Cell application
/// </summary>
public interface ICellHost : IRunnable, IAsyncDisposable
{
    /// <summary>
    /// Application dependencies
    /// </summary>
    IContainer Services { get; }
}