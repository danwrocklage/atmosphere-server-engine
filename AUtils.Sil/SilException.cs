using System;

namespace AUtils.Sil;

/// <summary>
/// Serialization exception
/// </summary>
public class SilException : Exception
{
    public SilException() { }
    public SilException(string message) : base(message) { }
    public SilException(string message, Exception innerException) : base(message, innerException) { }
}