using System.Net;
using System.Reflection;
using System.Security.Claims;
using ACore.Worker.Web;
using ACore.Worker.Web.Routing;
using AGame.Core.Identity;
using Fb.Web.Shared.Tokens;
using ClaimTypes = AGame.Core.Identity.ClaimTypes;

namespace Fb.Web.Shared;

/// <summary>
/// Common authentication middleware
/// </summary>
public abstract class AuthenticateMiddleware : Middleware
{
    private readonly IJwtService mJwtService;
    private readonly ITokenService mTokenService;

    protected AuthenticateMiddleware(IJwtService jwtService, ITokenService tokenService)
    {
        mJwtService = jwtService;
        mTokenService = tokenService;
    }

    protected virtual Task Challenge(HttpListenerContext context, Session session, ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default) => 
        Next(context, session, cancellationToken);

    public override async Task Execute(HttpListenerContext context, Session session, CancellationToken cancellationToken = default)
    {
        if (context.Request.HttpMethod == HttpMethods.OPTIONS
#if !Production
            || context.Request.Url?.LocalPath.StartsWith("/api/web-routing") == true
#endif
           )
        {
            await Next(context, session, cancellationToken);
            return;
        }
        
        var method = session.GetRouteAction(context);
        if (method == null)
        {
            await Next(context, session, cancellationToken);
            return;
        }

        var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() ?? 
                          method.DeclaringType?.GetCustomAttribute<AllowAnonymousAttribute>(true);
        if (isAnonymous != null)
        {
            await Next(context, session, cancellationToken);
            return;
        }

        var token = context.Request.GetJwt();
        if (string.IsNullOrEmpty(token) || await mTokenService.IsTokenRevoked(token))
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }
            
        var claims = mJwtService.GetPrincipal(token);
        if (claims == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }

        var entityId = claims.FindFirst(x => x.Type == ClaimTypes.EntityId)?.Value;
        if (entityId == null)
        {
            context.Response.StatusCode = 401;
            context.Response.Close();
            return;
        }
        session.SetEntityId(entityId);

        await Challenge(context, session, claims);
    }
}