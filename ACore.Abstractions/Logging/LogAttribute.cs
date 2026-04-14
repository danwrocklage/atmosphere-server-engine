namespace ACore.Abstractions.Logging;

/// <summary>
/// Additional information for logging
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class LogAttribute : Attribute
{
    /// <summary>
    /// Logging category
    /// </summary>
    public string Category { get; init; }
}