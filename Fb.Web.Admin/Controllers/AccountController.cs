using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Account;
using AGame.Core.Account.Models;

namespace Fb.Web.Admin.Controllers;

[RoutePrefix("accounts")]
[Role("accounts")]
public class AccountController : Controller
{
    private readonly IAccountService mAccountService;

    public AccountController(IAccountService accountService)
    {
        mAccountService = accountService;
    }

    [Get]
    [Description("Get all player's accounts")]
    public async Task GetAccounts([FromQuery] int page, [FromQuery] int size)
    {
        if (page <= 0 || size <= 0)
        {
            Response(400);
            return;
        }

        var accounts = await mAccountService.GetAccounts(new AccountFilter
        {
            Page = page,
            Size = size
        });

        await Response(accounts);
    }

    [Get("{id}")]
    [Description("Get account by id")]
    public async Task GetAccount([FromRoute] string id)
    {
        if (!Guid.TryParse(id, out var accountId))
        {
            Response(400);
            return;
        }

        var result = await mAccountService.GetAccountById(accountId);
        if (result == null)
        {
            Response(404);
            return;
        }
        
        await Response(result);
    }
    
    
}