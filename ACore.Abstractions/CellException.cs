namespace ACore.Abstractions;

public class CellException : ApplicationException
{
    public CellException() {}
    public CellException(string message) : base(message) {}
    public CellException(string message, Exception innerException) : base(message, innerException) {}
}