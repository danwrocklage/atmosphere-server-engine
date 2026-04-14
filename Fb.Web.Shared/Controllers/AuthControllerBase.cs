using System.ComponentModel;
using System.Security.Claims;
using ACore.Abstractions;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Identity;
using Fb.Web.Shared.Models.Token;
using Fb.Web.Shared.Tokens;

namespace Fb.Web.Shared.Controllers;

/// <summary>
/// Base of authentication controller
/// </summary>
[RoutePrefix("auth")]
public abstract class AuthControllerBase : Controller
{
    private readonly IIdentityService mIdentityService;
    private readonly IJwtService mJwtService;
    private readonly ITokenService mTokenService;

    protected AuthControllerBase(IIdentityService identityService, IJwtService jwtService, ITokenService tokenService)
    {
        mIdentityService = identityService;
        mJwtService = jwtService;
        mTokenService = tokenService;
    }

    /// <summary>
    /// Custom validation of authenticated user
    /// </summary>
    protected abstract Task<bool> Validate(GetTokenRequest request, Identity identity, bool shouldBeBlocked);

    /// <summary>
    /// Add custom claims to JWT
    /// </summary>
    protected virtual Task<Claim[]> GetAdditionClaims(Identity identity) => Task.FromResult(Array.Empty<Claim>());

    /// <summary>
    /// Get JWT from basic authorization
    /// </summary>
    [Post("token"), AllowAnonymous]
    [Description("Get JWT from basic authorization")]
    public async Task GetToken([FromBody] GetTokenRequest request)
    {
        Response(400);
        
        if (request == null ||
            string.IsNullOrEmpty(request.Login) ||
            string.IsNullOrEmpty(request.GrandType) ||
            string.IsNullOrEmpty(request.Password) ||
            !GrandTypes.Items.Contains(request.GrandType))
            return;

        var result = await mIdentityService.Authorize(request.Login, request.Password, true);
        if (result.Identity == null)
            return;

        if (!await Validate(request, result.Identity, result.ShouldBeBlocked))
            return;

        var claims = JwtServiceExtensions
            .GetClaimsByEntity((result.Identity.Link.Id, result.Identity.Link.Type, request.GrandType));
        var additionalClaims = await GetAdditionClaims(result.Identity) ?? throw new CellException();
        if (additionalClaims.Length > 0)
        {
            var oldSize = claims.Length;
            Array.Resize(ref claims, oldSize + additionalClaims.Length);
            additionalClaims.CopyTo(claims, oldSize);
        }
        
        var token = mJwtService.Generate(claims, out var expires);
        await Response(new GetTokenResponse
        {
            Token = token,
            ExpiredAt = expires
        });
    }

    /// <summary>
    /// Revoke current JWT
    /// </summary>
    [Delete("token")]
    [Description("Revoke current JWT")]
    public async Task RevokeToken()
    {
        await mTokenService.RevokeToken(Request.GetJwt());
    }

    /// <summary>
    /// Get new JWT from old expired JWT and refresh token
    /// </summary>
    [Put, Description("[NOT IMPLEMENTED] Get new JWT from old expired JWT and refresh token")]
    public Task RefreshToken([FromBody] string refreshToken)
    {
        Response(501);
        return Task.CompletedTask;
    }
}