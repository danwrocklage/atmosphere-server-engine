using Fb.Web.Shared.Models;

namespace Fb.Web.Shared;

public static class StatusResponses
{
    public static CommonStatusResponse BlockedByPassword { get; } = new()
    {
        Code = "BLOCKED.BY.PASSWORD",
        Message = "Login attempts ended and identity was blocked"
    };
    
    public static CommonStatusResponse NotFound { get; } = new()
    {
        Code = "NOT.FOUND",
        Message = "The requested item was not found"
    };
}