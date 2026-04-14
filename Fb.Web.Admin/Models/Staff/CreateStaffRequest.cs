namespace Fb.Web.Admin.Models.Staff;

public class CreateStaffRequest : EditStaffRequest
{
    public Guid IdentityId { get; set; }
}