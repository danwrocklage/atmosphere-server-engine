namespace Fb.Web.Shared.Models.Token;

public class GetTokenRequest
{
    public string Login { get; set; }
        
    public string Password { get; set; }
        
    public string GrandType { get; set; }
}