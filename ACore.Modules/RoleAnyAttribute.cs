namespace ACore.Modules;

/// <summary>
/// Method will run for all roles specified in this attribute.
/// If it empty - for any role.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RoleAnyAttribute : Attribute
{
    public string[] Cells { get; }

    public RoleAnyAttribute(params string[] cells)
    {
        Cells = cells;
    }
}