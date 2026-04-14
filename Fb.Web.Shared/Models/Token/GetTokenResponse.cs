namespace Fb.Web.Shared.Models.Token;

public class GetTokenResponse
{
    public string Token { get; set; }
        
    public DateTime ExpiredAt { get; set; }
}