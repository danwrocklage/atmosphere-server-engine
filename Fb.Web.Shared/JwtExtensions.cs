using System.Net;

namespace Fb.Web.Shared;

public static class JwtExtensions
{
    public static string GetJwt(this HttpListenerRequest request) => request.Headers.Get("Authorization");
}