namespace ACore.Abstractions.Worker;

/// <summary>
/// Describe running worker
/// </summary>
public class RunningWorker
{
    /// <summary>
    /// Is currently worker running?
    /// </summary>
    public bool IsRunning { get; set; }
    
    /// <summary>
    /// Worker type
    /// </summary>
    public string Type { get; set; }
        
    /// <summary>
    /// Developer defined name of worker
    /// </summary>
    public string Name { get; set; }
        
    /// <summary>
    /// Worker name from <see cref="WorkerAttribute"/>
    /// </summary>
    public string Worker { get; set; }
}