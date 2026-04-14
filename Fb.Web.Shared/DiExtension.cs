using AUtils.IoC;
using Fb.Web.Shared.Tokens;

namespace Fb.Web.Shared;

public static class DiExtension
{
    public static void AddWebSharedServices(this ContainerBuilder builder)
    {
        builder.Transient<TokenService, ITokenService>();
    }
}