namespace AUtils.IoC;

public class ResolveException : Exception
{
    /// <summary>
    /// Current building type
    /// </summary>
    public Type Type { get; }
        
    /// <summary>
    /// Unresolvable parameter of current type
    /// </summary>
    public Type Parameter { get; }

    public ResolveException(Type type) : base($"Type [{type.FullName}] is not registered")
    {
        Type = type;
    }
    
    public ResolveException(Type type, Type parameter) : base($"[Current:{type.Name}] Type [{parameter.FullName}] is not registered")
    {
        Type = type;
        Parameter = parameter;
    }
}