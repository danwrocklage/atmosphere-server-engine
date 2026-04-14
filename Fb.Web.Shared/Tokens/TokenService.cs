using ACore.Abstractions.Logging;
using ACore.Abstractions.Storage;

namespace Fb.Web.Shared.Tokens;

[StorageKey("token:revoked")]
internal record RevokedToken(string Jwt, DateTime Until);

[Log(Category = "tokens")]
internal class TokenService : ITokenService
{
    private readonly IStorageList<RevokedToken> mRevokedTokens;
    private readonly ILogger<TokenService> mLogger;

    public TokenService(IStorageList<RevokedToken> revokedTokens, ILogger<TokenService> logger)
    {
        mRevokedTokens = revokedTokens;
        mLogger = logger;
    }


    public async Task RevokeToken(string token)
    {
        
    }

    public async Task<bool> IsTokenRevoked(string token)
    {
        return false;
    }

    public async Task RemoveExpiredTokens()
    {
        
    }
}