namespace AGame.Core;

internal static class CacheTopic
{
    internal static class Identity
    {
        internal static string Keys => "identity:keys";
        internal static string Fails => "identity:fails:count";
        internal static string BlockedUntil => "identity:blocked:until";
    }

    internal static string RevokedTokens => "token:revoked";
        
    internal static class Staff
    {
        internal static string RoleScopes => "staff:role:scopes";
        internal static string Shorts => "staff:shorts";
    }
}