namespace AGame.Core.Staff.Models;

public class EditStaff
{
    public string Name { get; set; }
        
    public string Email { get; set; }
        
    public string AvatarUrl { get; set; }

    public Guid RoleId { get; set; }
}