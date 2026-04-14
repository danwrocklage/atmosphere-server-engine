namespace AGame.Core.ClientApp;

/// <summary>
/// Managing client application versions
/// </summary>
public interface IClientBuildService
{
    Task CreateNewVersion(NewClientBuild model);
    
    Task<List<ClientBuildItem>> GetVersions();
    
    Task<bool> IsVersionSupported(string version);

    Task<string> GetCurrentVersion(ClientBuildType type);

    Task ChangeType(Guid id, ClientBuildType type);

    Task DeleteVersion(Guid id);
}