using System.Collections.Specialized;

namespace ACore.Abstractions;

/// <summary>
/// Interface for handling control command through HTTP
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Process control command
    /// </summary>
    Task<object> Run(NameValueCollection queryParams, CancellationToken token);
}