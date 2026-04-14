namespace Fb.Web.Admin;

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public class RoleAttribute : Attribute
{
    public RoleAttribute(string scope)
    {
        Scope = scope;
    }

    public string Scope { get; }
}