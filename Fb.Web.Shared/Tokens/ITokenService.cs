namespace Fb.Web.Shared.Tokens;

public interface ITokenService
{
    Task RevokeToken(string token);
    Task<bool> IsTokenRevoked(string token);
    Task RemoveExpiredTokens();
}