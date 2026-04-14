namespace ACore.Modules;

/// <summary>
/// Initialization run order
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OrderAttribute : Attribute
{
    public OrderAttribute(int order)
    {
        Order = order;
    }

    public int Order { get; }
}