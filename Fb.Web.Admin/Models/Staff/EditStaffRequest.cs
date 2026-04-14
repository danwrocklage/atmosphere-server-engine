namespace Fb.Web.Admin.Models.Staff;

public class EditStaffRequest
{
    public string Name { get; set; }
        
    public string Email { get; set; }
        
    public string AvatarUrl { get; set; }

    public Guid RoleId { get; set; }
}