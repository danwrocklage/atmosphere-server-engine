namespace Fb.Mechanics;

public class MechanicException : ApplicationException
{
    public MechanicException() {}
    public MechanicException(string message) : base(message) {}
    public MechanicException(string message, Exception innerException) : base(message, innerException) {}
}