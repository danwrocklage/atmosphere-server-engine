using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Worker;
using Fb.Web.Shared.Tokens;

namespace Fb.Web.Portal;

[Log(Category = "tokens")]
[Worker("revoked-tokens-remove")]
public class RemoveRevokedTokensWorker : IRunnable
{
    private readonly ITokenService mTokenService;

    public RemoveRevokedTokensWorker(ITokenService tokenService)
    {
        mTokenService = tokenService;
    }

    public Task Run(CancellationToken token) => mTokenService.RemoveExpiredTokens();
}