using AGame.Core.Identity;
using Fb.Web.Shared;
using Fb.Web.Shared.Tokens;

namespace Fb.Web.Portal;

public class AccountAuthenticateMiddleware : AuthenticateMiddleware
{
    public AccountAuthenticateMiddleware(IJwtService jwtService, ITokenService tokenService) 
        : base(jwtService, tokenService)
    {
    }
}