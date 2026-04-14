using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Staff;
using AGame.Core.Staff.Models;
using Fb.Web.Admin.Models.StaffRole;

namespace Fb.Web.Admin.Controllers;

public class StaffRoleController : BaseAdminController
{
    private readonly IStaffService mStaffService;

    public StaffRoleController(IStaffService staffService)
    {
        mStaffService = staffService;
    }

    [Get]
    [Description("Get all staff roles")]
    public async Task GetRoles([FromQuery] int page, [FromQuery] int size)
    {
        if (page <= 0 || size <= 0)
        {
            Response(400);
            return;
        }

        var roles = await mStaffService.GetRoles(new StaffRoleFilter
        {
            Page = page,
            Size = size
        });

        await Response(roles);
    }

    [Post]
    [Description("Create new staff role")]
    public async Task CreateRole([FromBody] CreateRoleRequest model)
    {
        if (model == null || string.IsNullOrEmpty(model.Name) || 
            model.Scopes == null || model.Scopes.Length == 0)
        {
            Response(400);
            return;
        }

        await mStaffService.CreateRole(model.Name, model.Scopes, StaffId);
    }

    [Post("{id}")]
    public async Task Edit([FromRoute] string id, [FromBody] CreateRoleRequest model)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }
        
        if (model == null || string.IsNullOrEmpty(model.Name) || 
            model.Scopes == null || model.Scopes.Length == 0)
        {
            Response(400);
            return;
        }

        await mStaffService.EditRole(staffId, model.Name, model.Scopes, StaffId);
    }
}