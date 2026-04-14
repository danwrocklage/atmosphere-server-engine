using ACore.Abstractions;
using ACore.Application.Configuration;

namespace ACore.Application;

internal class CellEnvironment : ICellEnvironment
{
    public CellEnvironment(CellBuildConfiguration buildConfiguration, IConfiguration configuration)
    {
        Role = buildConfiguration.Role;
        Configuration = buildConfiguration.Configuration;
        Build = buildConfiguration.Build;
        Endpoint = configuration.Get("ENDPOINT", () => System.Net.IPAddress.Loopback.ToString());
    }

    public string Role { get; }
    
    public string Configuration { get; }
    
    public string Build { get; }
    
    public string Endpoint { get; }

#if CONTAINER
    public bool IsContainerBuild => true;
#else
    public bool IsContainerBuild => false;
#endif

    public override string ToString() => 
        ((ICellEnvironment)this).ToString(false);
}