using System.ComponentModel;
using ACore.Patching;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using Fb.Web.Admin.Models;

namespace Fb.Web.Admin.Controllers;

[RoutePrefix("patch")]
[Role("patches")]
public class PatchController : Controller
{
    private readonly IPatchService mPatchService;

    public PatchController(IPatchService patchService)
    {
        mPatchService = patchService;
    }

    [Get]
    [Description("Get all patches filtered by category")]
    public async Task GetPatches([FromQuery] string category = null)
    {
        var patches = await mPatchService.GetPatches(category);

        var result = patches
            .Select(x => new PatchResponse
            {
                Category = x.Category,
                Name = x.Name,
                Order = x.Order,
                AppliedAt = x.AppliedAt,
                ClrType = x.ClrType,
                HasInCode = x.HasInCode
            })
            .ToArray();
        
        await Response(result);
    }
}