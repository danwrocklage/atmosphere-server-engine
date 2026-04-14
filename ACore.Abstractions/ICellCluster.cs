using ACore.Abstractions.Rpc;

namespace ACore.Abstractions;

/// <summary>
/// Manage cells information
/// </summary>
public interface ICellCluster
{
    public delegate Task CellClusterModified(CellInfo cellRole);

    public delegate Task CellErrorReceived(CellError cellError);

    /// <summary>
    /// Collection of running cell at this moment
    /// </summary>
    IReadOnlyCollection<CellInfo> Cells { get; }

    /// <summary>
    /// Get endpoint (dns name or ip address) for first available cell instance with specified role
    /// </summary>
    string GetRoleEndpoint(Guid cellId);

    /// <summary>
    /// Return true, if any cell instance with specified role is running and connected
    /// </summary>
    bool IsRoleAvailable(string cellRole);

    /// <summary>
    /// Return true, if cell id is existing (running) now
    /// </summary>
    bool IsCellIdExists(Guid cellId);
    
    /// <summary>
    /// When new cell was discovered
    /// </summary>
    event CellClusterModified CellFound;
    
    /// <summary>
    /// When cell lost connection
    /// </summary>
    event CellClusterModified CellLost;

    /// <summary>
    /// When cell in cluster (include self) crash with error
    /// </summary>
    event CellErrorReceived CellError;
}

[Topic(RpcType.Fanout)]
public class CellInfo
{
    /// <summary>
    /// Cell unique id
    /// </summary>
    public Guid AppId { get; set; }
    
    /// <summary>
    /// High level role of cell
    /// </summary>
    public string Role { get; set; }
    
    /// <summary>
    /// Cell environment configuration
    /// </summary>
    /// <remarks>
    /// Configurations are:
    /// - Development
    /// - Staging
    /// - Production
    /// </remarks>
    public string Configuration { get; set; }
    
    /// <summary>
    /// Cell current build version
    /// </summary>
    public string Build { get; set; }
    
    /// <summary>
    /// Cell endpoint
    /// </summary>
    public string Endpoint { get; set; }

    public override string ToString() => 
        $"{Role}.{Configuration}.{Build} {Endpoint} {AppId}";
}

[Topic("cell.error", RpcType.Fanout)]
public class CellError
{
    public Guid AppId { get; set; }
    public CellInfo Info { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
