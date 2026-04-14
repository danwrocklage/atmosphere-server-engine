namespace AGame.Frontend.Queue;

public interface IConnectionManager
{
    int TotalConnections { get; }
    
    Task<Guid?> ReserveConnection(Guid entityId);
}