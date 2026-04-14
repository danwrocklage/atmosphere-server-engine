using ACore.Abstractions;
using ACore.Abstractions.Worker;
using AGame.Core.Account;

namespace Fb.Web.Portal;

[Worker("unblock-accounts-by-pass")]
public class UnblockAccountsByPassword : IRunnable
{
    private readonly IAccountAccessService mAccountAccessService;

    public UnblockAccountsByPassword(IAccountAccessService accountAccessService)
    {
        mAccountAccessService = accountAccessService;
    }

    public Task Run(CancellationToken token) => mAccountAccessService.UnblockAccountsByPassword();
}