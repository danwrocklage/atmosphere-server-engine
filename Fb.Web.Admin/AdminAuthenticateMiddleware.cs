using System.Net;
using System.Reflection;
using System.Security.Claims;
using ACore.Worker.Web;
using AGame.Core.Identity;
using Fb.Web.Shared;
using Fb.Web.Shared.Tokens;
using ClaimTypes = AGame.Core.Identity.ClaimTypes;

namespace Fb.Web.Admin;

public class AdminAuthenticateMiddleware : AuthenticateMiddleware
{
    public AdminAuthenticateMiddleware(IJwtService jwtService, ITokenService tokenService) 
        : base(jwtService, tokenService)
    {
    }

    protected override Task Challenge(HttpListenerContext context, Session session, ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken = default)
    {
        var action = session.GetRouteAction(context);
        var scope = action.GetCustomAttribute<RoleAttribute>()?.Scope ??
                    action.DeclaringType?.GetCustomAttribute<RoleAttribute>()?.Scope;

        if (string.IsNullOrEmpty(scope))
            return Next(context, session, cancellationToken);
        
        var userScopes = claimsPrincipal.FindFirst(x => x.Type == ClaimTypes.Scopes)?.Value;
        if (string.IsNullOrEmpty(userScopes))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return Task.CompletedTask;
        }

        var scopes = userScopes.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        if (scopes.Length <= 0)
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return Task.CompletedTask;
        }
        
        if(!scopes.Contains(scope))
        {
            context.Response.StatusCode = 403;
            context.Response.Close();
            return Task.CompletedTask;
        }
        
        return Next(context, session, cancellationToken);
    }
}