namespace ACore.Modules;

/// <summary>
/// Method will call for all roles except specified in this attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RoleExceptAttribute : Attribute
{
    public string[] Cells { get; }

    public RoleExceptAttribute(params string[] cells)
    {
        Cells = cells;
    }
}