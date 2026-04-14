using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Account;
using AGame.Core.Account.Models;
using AGame.Core.Identity;
using Fb.Web.Portal.Models.Account;
using Fb.Web.Shared;
using AccountUpdate = Fb.Web.Portal.Models.Account.AccountUpdate;

namespace Fb.Web.Portal.Controllers;

public class AccountController : BasePortalController
{
    private readonly IAccountService mAccountService;
    private readonly IIdentityService mIdentityService;

    public AccountController(IAccountService accountService, IIdentityService identityService)
    {
        mAccountService = accountService;
        mIdentityService = identityService;
    }

    /// <summary>
    /// Register new account and send activation token
    /// </summary>
    [Post("new")]
    [AllowAnonymous]
    [Description("Create new account")]
    public async Task CreateNewAccount([FromBody] CreateAccountRequest request)
    {
        if (request == null ||
            string.IsNullOrEmpty(request.Email) ||
            string.IsNullOrEmpty(request.Login) ||
            string.IsNullOrEmpty(request.Password) ||
            string.IsNullOrEmpty(request.Name))
        {
            Response(400);
            return;
        }

        if (await mIdentityService.Exists(request.Login, IdentityType.LoginPassword))
        {
            Response(400);
            return;
        }
        
        if (await mAccountService.IsEmailExists(request.Email))
        {
            Response(400);
            return;
        }
        
        var identityId = await mIdentityService.Create(request.Login, request.Password, IdentityType.LoginPassword,
            new[] {GrandTypes.Web, GrandTypes.Client});

        var activationToken = await mAccountService.CreateNewAccount(new AccountCreate
        {
            Source = RegistrationSource.Portal,
            IdentityId = identityId,
            Email = request.Email,
            Name = request.Name,
            EmailSubscription = request.EmailSubscription
        });
        
#if !Production
        await Response(new {ActivationToken = activationToken});
#endif
    }
    
    [Post("activate")]
    [AllowAnonymous]
    [Description("Activate just created account")]
    public async Task ActivateAccount([FromBody] ActivateAccountRequest body)
    {
        if (body == null || string.IsNullOrEmpty(body.Code))
        {
            Response(400);
            return;
        }

        var result = await mAccountService.ActivateAccount(body.Code);
        Response(result ? 200 : 400);
    }
    
    [Get]
    [Description("Get my own account information")]
    public async Task<AccountResponse> GetMyAccount()
    {
        var account = await mAccountService.GetAccountById(AccountId);
        return new AccountResponse
        {
            Email = account.Email,
            Name = account.Name,
            AvatarUrl = account.AvatarUrl,
            CreateAt = account.CreatedAt,
            LastActive = account.LastActive
        };
    }
    
    [Get("{id}")]
    [Description("Get name and avatar of any account")]
    public async Task<AccountShortResponse> GetAccount(Guid id)
    {
        var account = await mAccountService.GetShortAccountById(id);
        if (account != null)
            return new AccountShortResponse
            {
                Name = account.Name,
                AvatarUrl = account.AvatarUrl
            };
            
        Response(404);
        return null;
    }

    [Patch]
    [Description("Update own account")]
    public async Task UpdateAccount([FromBody] AccountUpdate body)
    {
        if (body == null)
        {
            Response(400);
            return;
        }

        await mAccountService.UpdateAccount(new AGame.Core.Account.Models.AccountUpdate
        {
            Email = body.Email,
            Name = body.Name,
            AccountId = AccountId,
            AvatarUrl = body.AvatarUrl
        });
    }
}