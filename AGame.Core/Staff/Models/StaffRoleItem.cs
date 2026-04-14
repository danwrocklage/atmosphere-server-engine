namespace AGame.Core.Staff.Models;

public class StaffRoleItem
{
    public Guid Id { get; set; }
        
    public string Name { get; set; }
        
    public string[] Scopes { get; set; }
}