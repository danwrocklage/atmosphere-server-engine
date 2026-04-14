using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.ClientApp;
using AGame.Core.Identity;
using Fb.Web.Admin.Models.ClientBuild;
using Fb.Web.Shared;

namespace Fb.Web.Admin.Controllers;

[RoutePrefix("client-build")]
[Role("client-builds")]
public class ClientBuildController : Controller
{
    private readonly IClientBuildService mClientBuildService;
    private readonly IJwtService mJwtService;

    public ClientBuildController(IClientBuildService clientBuildService, IJwtService jwtService)
    {
        mClientBuildService = clientBuildService;
        mJwtService = jwtService;
    }

    [Get]
    [Description("Get list of all uploaded versions")]
    public async Task GetVersions()
    {
        var items = await mClientBuildService.GetVersions();
        await Response(items.Select(x => new ClientBuildResponse
        {
            Id = x.Id,
            BuildType = x.BuildType,
            Type = x.Type,
            Version = x.Version,
            CreatedAt = x.CreatedAt
        }));
    }

    [Post, AllowAnonymous]
    [Description("Create new version of client application")]
    public async Task CreateNewVersion([FromBody] NewClientBuildRequest request, [FromHeader("Authorization")] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Response(401);
            return;
        }

        var tokenPrincipal = mJwtService.GetPrincipal(token);
        if(!Enum.TryParse(tokenPrincipal?.FindFirst(ClaimTypes.ClientType)?.Value, out ClientType clientType))
        {
            Response(401);
            return;
        }
        
        if(request == null || !request.IsValid())
        {
            Response(400);
            return;
        }
        
        await mClientBuildService.CreateNewVersion(new NewClientBuild
        {
            BuildType = request.BuildType,
            Type = clientType,
            Version = request.Version
        });
    }

    [Get("current")]
    [Description("Get current actual client build of specified type")]
    public async Task GetCurrentVersion([FromQuery] ClientBuildType type)
    {
        await Response(await mClientBuildService.GetCurrentVersion(type));
    }

    [Post("{id}")]
    [Description("Change client build type")]
    public async Task ChangeType([FromRoute] string id, [FromBody] ChangeClientBuildTypeRequest request)
    {
        if (!Guid.TryParse(id, out var buildId))
        {
            Response(400);
            return;
        }
        
        await mClientBuildService.ChangeType(buildId, request.BuildType);
    }

    [Delete("{id}")]
    [Description("Delete client build")]
    public async Task DeleteVersion([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var buildId))
        {
            Response(400);
            return;
        }

        await mClientBuildService.DeleteVersion(buildId);
    }
}