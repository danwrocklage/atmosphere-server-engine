using System.Security.Claims;
using AGame.Core.Identity;
using AGame.Core.Staff;
using Fb.Web.Shared;
using Fb.Web.Shared.Controllers;
using Fb.Web.Shared.Models.Token;
using Fb.Web.Shared.Tokens;
using ClaimTypes = AGame.Core.Identity.ClaimTypes;

namespace Fb.Web.Admin.Controllers;

public class StaffAuthController : AuthControllerBase
{
    private readonly IStaffService mStaffService;
    
    public StaffAuthController(IIdentityService identityService, IJwtService jwtService, 
        ITokenService tokenService, IStaffService staffService) :
        base(identityService, jwtService, tokenService)
    {
        mStaffService = staffService;
    }

    protected override async Task<bool> Validate(GetTokenRequest request, Identity identity, bool shouldBeBlocked)
    {
        if (request.GrandType != GrandTypes.WebAdmin)
        {
            Response(400);
            return false;
        }
        
        var canAuth = await mStaffService.CanBeAuthenticated(identity.Link.Id);
        if (!canAuth.HasValue)
        {
            await Response(StatusResponses.NotFound, 404);
            return false;
        }

        if (!canAuth.Value)
        {
            await Response(StatusResponses.BlockedByPassword, 401);
            return false;
        }
        
        if (shouldBeBlocked)
        {
            await mStaffService.Deactivate(identity.Link.Id);
            await Response(StatusResponses.BlockedByPassword, 401);
            return false;
        }

        return true;
    }

    protected override async Task<Claim[]> GetAdditionClaims(Identity identity)
    {
        var scopes = await mStaffService.GetStaffRoleScopes(identity.Link.Id);
        return new[] {new Claim(ClaimTypes.Scopes, string.Join(',', scopes))};
    }
}