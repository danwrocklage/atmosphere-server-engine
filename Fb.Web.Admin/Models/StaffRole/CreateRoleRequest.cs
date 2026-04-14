namespace Fb.Web.Admin.Models.StaffRole;

public class CreateRoleRequest
{
    public string Name { get; set; }
    
    public string[] Scopes { get; set; }
}