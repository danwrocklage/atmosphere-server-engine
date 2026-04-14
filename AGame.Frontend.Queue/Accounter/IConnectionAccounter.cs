namespace AGame.Frontend.Queue;

public interface IConnectionAccounter
{
    bool IsAvailable { get; }
    
    /// <summary>
    /// Increment connections count, if it can
    /// </summary>
    /// <returns>Return true, if connections count was incremented, otherwise - false</returns>
    Task<IAsyncDisposable> Reserve();

    bool IsWaiting(Guid entityId);
}