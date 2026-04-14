using AGame.Core.ClientApp;

namespace Fb.Web.Admin.Models.ClientBuild;

public class ClientBuildResponse
{
    public Guid Id { get; set; }
    
    public string Version { get; set; }
    
    public ClientType Type { get; set; }
    
    public ClientBuildType BuildType { get; set; }
    
    public DateTime CreatedAt { get; set; }
}