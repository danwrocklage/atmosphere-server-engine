using AGame.Core.Account;
using AGame.Core.Identity;
using Fb.Web.Shared;
using Fb.Web.Shared.Controllers;
using Fb.Web.Shared.Models.Token;
using Fb.Web.Shared.Tokens;

namespace Fb.Web.Portal.Controllers;

public class AccountAuthController : AuthControllerBase
{
    private readonly IAccountService mAccountService;
    private readonly IAccountAccessService mAccountAccessService;

    public AccountAuthController(IIdentityService identityService, IJwtService jwtService, ITokenService tokenService,
        IAccountAccessService accountAccessService, IAccountService accountService) :
        base(identityService, jwtService, tokenService)
    {
        mAccountAccessService = accountAccessService;
        mAccountService = accountService;
    }

    protected override async Task<bool> Validate(GetTokenRequest request, Identity identity, bool shouldBeBlocked)
    {
        if (request.GrandType != GrandTypes.Web)
        {
            Response(400);
            return false;
        }
        
        var canAuth = await mAccountAccessService.CanAuthenticate(identity.Link.Id);
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
            await mAccountService.ChangeStatus(new AccountChangeStatus
            {
                Until = DateTime.UtcNow.AddMinutes(30),
                AccountId = identity.Link.Id,
                Status = AccountStatus.BlockedByPassword
            });

            await Response(StatusResponses.BlockedByPassword, 400);
            return false;
        }

        await mAccountService.UpdateAccountActivity(identity.Link.Id);
        return true;
    }
}