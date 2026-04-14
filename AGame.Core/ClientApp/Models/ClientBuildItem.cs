namespace AGame.Core.ClientApp;

public class ClientBuildItem
{
    public Guid Id { get; set; }
    
    public string Version { get; set; }
    
    public ClientBuildType BuildType { get; set; }
    
    public ClientType Type { get; set; }
    
    public DateTime CreatedAt { get; set; }
}