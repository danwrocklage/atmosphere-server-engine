using AGame.Core.ClientApp;

namespace Fb.Web.Admin.Models.ClientBuild;

public class NewClientBuildRequest
{
    public string Version { get; set; }
    
    public ClientBuildType BuildType { get; set; }

    public bool IsValid() =>
        !string.IsNullOrEmpty(Version) && 
        Enum.IsDefined(BuildType);
}