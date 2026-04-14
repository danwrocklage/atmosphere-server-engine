using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Staff;
using AGame.Core.Staff.Models;
using Fb.Web.Admin.Models.Staff;
using EditStaff = AGame.Core.Staff.Models.EditStaff;

namespace Fb.Web.Admin.Controllers;

public class StaffController : BaseAdminController
{
    private readonly IStaffService mStaffService;

    public StaffController(IStaffService staffService)
    {
        mStaffService = staffService;
    }

    [Get]
    [Description("Get all staffs")]
    public async Task GetStaffs([FromQuery] int page, [FromQuery] int size)
    {
        if (page <= 0 || size <= 0)
        {
            Response(400);
            return;
        }
        
        var result = await mStaffService.GetStaffs(new StaffFilter
        {
            Page = page,
            Size = size
        });

        await Response(result);
    }

    [Get("{id}")]
    [Description("Get staff by item")]
    public async Task GetStaff([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }
        
        var result = await mStaffService.GetStaff(staffId);
        await Response(result);
    }

    [Post]
    [Description("Create new staff")]
    public async Task CreateStaff([FromBody] CreateStaffRequest model)
    {
        var result = await mStaffService.Create(new CreateStaff
        {
            Email = model.Email,
            Name = model.Name,
            AvatarUrl = model.AvatarUrl,
            IdentityId = model.IdentityId,
            RoleId = model.RoleId
        });

        if (!result.HasValue)
        {
            Response(400);
            return;
        }

        await Response(result.Value);
    }
    
    [Post("{id}")]
    [Description("Update new staff")]
    public async Task UpdateStaff([FromRoute] string id, [FromBody] EditStaffRequest model)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }
        
        await mStaffService.Edit(staffId, new EditStaff
        {
            Email = model.Email,
            Name = model.Name,
            AvatarUrl = model.AvatarUrl,
            RoleId = model.RoleId
        });
    }

    [Post("{id}/activate")]
    public async Task Activate([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }

        await mStaffService.Activate(staffId, StaffId);
    }

    [Post("{id}/deactivate")]
    public async Task Deactivate([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }

        await mStaffService.Deactivate(staffId);
    }
    
    [Delete("{id}")]
    public async Task Delete([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var staffId))
        {
            Response(400);
            return;
        }

        await mStaffService.Delete(staffId, StaffId);
    }
}