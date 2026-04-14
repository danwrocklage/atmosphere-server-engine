using System.Reflection;

namespace ACore.Abstractions.Worker;

/// <summary>
/// Cell workers manager
/// </summary>
public interface ICellWorkers
{
    public const int DEFAULT_FAIL_TIMES = 3;
    
    /// <summary>
    /// Run a scheduled worker of <see cref="type"/> with <see cref="name"/>
    /// </summary>
    /// <param name="name">Name of worker (for display and stopping)</param>
    /// <param name="type">Type of worker. See <see cref="WorkerAttribute"/></param>
    /// <param name="token">Cancellation token</param>
    /// <param name="cron">Cron expression for schedule</param>
    void Run(string name, string type, CancellationToken token, string cron);

    /// <summary>
    /// Run regular worker (run only once) of <see cref="type"/> with <see cref="name"/>
    /// </summary>
    /// <param name="name">Name of worker (for display and stopping)</param>
    /// <param name="type">Type of worker. See <see cref="WorkerAttribute"/></param>
    /// <param name="token">Cancellation token</param>
    /// <param name="failTimes">Max exceptions count for restarting worker</param>
    void Run(string name, string type, CancellationToken token, int failTimes = DEFAULT_FAIL_TIMES);

    /// <summary>
    /// Run a scheduled worker of <see cref="T"/> with <see cref="name"/>
    /// </summary>
    /// <param name="name">Name of worker (for display and stopping)</param>
    /// <param name="token">Cancellation token</param>
    /// <param name="cron">Cron expression for schedule</param>
    public void Run<T>(string name, CancellationToken token, string cron) where T : IRunnable
    {
        var workerType = typeof(T).GetCustomAttribute<WorkerAttribute>()?.Name;
        
        if (string.IsNullOrEmpty(workerType))
            throw new ArgumentNullException();
        
        Run(name, workerType, token, cron);
    }

    /// <summary>
    /// Run regular worker (run only once) of <see cref="T"/> with <see cref="name"/>
    /// </summary>
    /// <param name="name">Name of worker (for display and stopping)</param>
    /// <param name="token">Cancellation token</param>
    /// <param name="failTimes">Max exceptions count for restarting worker</param>
    public void Run<T>(string name, CancellationToken token, int failTimes = DEFAULT_FAIL_TIMES) where T : IRunnable
    {
        var workerType = typeof(T).GetCustomAttribute<WorkerAttribute>()?.Name;
        
        if (string.IsNullOrEmpty(workerType))
            throw new ArgumentNullException();
        
        Run(name, workerType, token, failTimes);
    }

    /// <summary>
    /// Stop worker with <see cref="name"/>
    /// </summary>
    void Stop(string name);

    /// <summary>
    /// Get list of running workers at current moment
    /// </summary>
    RunningWorker[] GetRunningWorkers();

    /// <summary>
    /// When new worker runs first time
    /// </summary>
    event Func<string, Task> OnStart;
    
    /// <summary>
    /// When worker stopped
    /// </summary>
    event Func<string, Task> OnStop;
    
    /// <summary>
    /// When worker throws exception
    /// </summary>
    event Func<string, Exception, Task> OnError;

    /// <summary>
    /// Get total running workers count on all cells
    /// </summary>
    int GetAllWorkerCount();
    
    /// <summary>
    /// Get total running workers with specified name count on all cells
    /// </summary>
    int GetAllWorkerCount(string workerName);

    /// <summary>
    /// Return true, if at least one worker on all cells is running
    /// </summary>
    public bool IsAnyWorkerRunning() => GetAllWorkerCount() > 0;

    /// <summary>
    /// Return true, if at least one worker with specified name on all cells is running
    /// </summary>
    public bool IsAnyWorkerRunning(string workerName) => GetAllWorkerCount(workerName) > 0;
}