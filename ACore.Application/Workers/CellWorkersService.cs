using System.Collections.Concurrent;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;
using AUtils.IoC;
using Cronos;

namespace ACore.Application.Workers;

/// <inheritdoc/>
[Log(Category = "workers")]
internal partial class CellWorkersService : ICellWorkers
{
    internal static readonly Dictionary<WorkerType, string> WorkerTypes =
        Enum.GetValues<WorkerType>().ToDictionary(x => x, x => Enum.GetName(x));

    private readonly ConcurrentDictionary<string, WorkerInfo> mWorkerThreadInfos;
    private readonly ILogger<CellWorkersService> mLogger;
    private readonly IContainer mContainer;
    private readonly IRpc mRpc;

    public CellWorkersService(ILogger<CellWorkersService> logger, IContainer container, IRpc rpc)
    {
        mLogger = logger;
        mContainer = container;
        mRpc = rpc;
        mWorkerThreadInfos = new ConcurrentDictionary<string, WorkerInfo>();
    }

    /// <inheritdoc/>
    public void Run(string name, string workerType, CancellationToken token, string cron)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (string.IsNullOrEmpty(cron))
            throw new ArgumentNullException(nameof(cron));

        var type = GetWorkerType(workerType);
        if (type == null)
            throw new ArgumentNullException(nameof(workerType));

        if (mWorkerThreadInfos.ContainsKey(name))
        {
            mLogger.Warn($"Worker with name '{name}' is already running");
            return;
        }

        if (!mWorkerThreadInfos.TryAdd(name, new WorkerInfo
            {
                IsAlive = false,
                Source = CancellationTokenSource.CreateLinkedTokenSource(token),
                Type = workerType,
                WorkerType = WorkerType.Cron
            }))
        {
            mLogger.Warn($"Failed to add worker '{name}' to workers dictionary");
            return;
        }

        // ReSharper disable once AsyncVoidLambda
        ThreadPool.QueueUserWorkItem(async state =>
        {
            var (stateName, stateCron, stateType) = (Tuple<string, string, Type>) state ?? throw new Exception();
            try
            {
                var isFirstRun = true;
                var cronScheduler = CronExpression.Parse(stateCron, CronFormat.IncludeSeconds);
                var runnable = (IRunnable) mContainer.Resolve(stateType);

                var taskToken = mWorkerThreadInfos[stateName].Source.Token;
                while (!taskToken.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    var waitTime = cronScheduler.GetNextOccurrence(now);

                    if (!waitTime.HasValue)
                    {
                        mLogger.Info($"Cron worker '{stateName}' [{stateType.FullName}] was stopped");
                        if (OnStop != null)
                            await OnStop.Invoke(stateName).ConfigureAwait(false);
                        return;
                    }

                    await Task.Delay(waitTime.Value - now, taskToken);

                    mWorkerThreadInfos[stateName].IsAlive = true;
                    if(mRpc != null)
                        await mRpc.Call(new WorkerEvent {Name = stateName, IsRunning = true}, taskToken);

                    if (isFirstRun)
                    {
                        mLogger.Debug($"Cron worker '{stateName}' [{stateType.FullName}] is starting");
                        if (OnStart != null)
                            await OnStart.Invoke(stateName).ConfigureAwait(false);
                        isFirstRun = false;
                    }

                    await runnable.Run(taskToken);

                    mWorkerThreadInfos[stateName].IsAlive = false;
                    if(mRpc != null)
                        await mRpc.Call(new WorkerEvent {Name = stateName, IsRunning = false}, taskToken);
                }

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (runnable is IDisposable disposableRunnable)
                    disposableRunnable.Dispose();

                if (runnable is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();

                mLogger.Info($"Cron worker '{stateName}' [{stateType.FullName}] was stopped");
                if (OnStop != null)
                    await OnStop.Invoke(stateName).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                mLogger.Info($"Cron worker '{stateName}' [{stateType.FullName}] was cancelled");
                if (OnStop != null)
                    await OnStop.Invoke(stateName).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                mLogger.Error($"Cron worker '{stateName}' [{stateType.FullName}] was failed", e);
                if (OnError != null)
                    await OnError.Invoke(stateName, e).ConfigureAwait(false);
            }
            finally
            {
                if (!mWorkerThreadInfos.TryRemove(stateName, out _))
                    mLogger.Warn(
                        $"Cron worker '{stateName}' [{stateType.FullName}] can't be removed from list");
            }
        }, new Tuple<string, string, Type>(name, cron, type));
    }

    /// <inheritdoc/>
    public void Run(string name, string workerType, CancellationToken token, int failTimes = ICellWorkers.DEFAULT_FAIL_TIMES)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        var type = GetWorkerType(workerType);
        if (type == null)
            throw new ArgumentNullException(nameof(workerType));
        
        if (mWorkerThreadInfos.ContainsKey(name))
        {
            mLogger.Log($"Worker with name {name} is already used", LogLevel.Warning);
            return;
        }

        if (!mWorkerThreadInfos.TryAdd(name, new WorkerInfo
            {
                IsAlive = true,
                Source = CancellationTokenSource.CreateLinkedTokenSource(token),
                Type = workerType,
                WorkerType = WorkerType.Regular
            }))
        {
            mLogger.Warn($"Failed to add worker '{name}' to workers dictionary");
            return;
        }

        // ReSharper disable once AsyncVoidLambda
        ThreadPool.QueueUserWorkItem(async state =>
        {
            var (stateName, stateFailTimes, stateType) = (Tuple<string, int, Type>) state ?? throw new Exception();
            var taskToken = mWorkerThreadInfos[stateName].Source.Token;
            var currentTime = 0;
            while (currentTime <= stateFailTimes)
            {
                try
                {
                    mLogger.Info($"{(currentTime > 0 ? "Rerun" : "Run")} {stateName} worker [{stateType.FullName}]");
                    if(mRpc != null)
                        await mRpc.Call(new WorkerEvent {Name = stateName, IsRunning = true}, taskToken);
                    
                    if (OnStart != null)
                        await OnStart.Invoke(stateName).ConfigureAwait(false);

                    var runnable = (IRunnable) mContainer.Resolve(stateType);
                    await runnable.Run(taskToken);

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (runnable is IDisposable disposableRunnable)
                        disposableRunnable.Dispose();
                    if (runnable is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();

                    if(mRpc != null)
                        await mRpc.Call(new WorkerEvent {Name = stateName, IsRunning = false}, taskToken);
                    mLogger.Info($"{stateName} worker was stopped");
                    if (OnStop != null)
                        await OnStop.Invoke(stateName).ConfigureAwait(false);
                    break;
                }
                catch (TaskCanceledException)
                {
                    mLogger.Info($"Worker {stateName} [{stateType.FullName}] was cancelled");
                    if (OnStop != null)
                        await OnStop.Invoke(stateName).ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    mLogger.Error($"Error on worker {stateName} [{stateType.FullName}]", e);
                    if (OnError != null)
                        await OnError.Invoke(stateName, e).ConfigureAwait(false);
                    currentTime++;
                }
            }

            if (!mWorkerThreadInfos.TryRemove(stateName, out _))
                mLogger.Warn(
                    $"Cron worker '{stateName}' [{stateType.FullName}] can't be removed from list");
        }, new Tuple<string, int, Type>(name, failTimes, type));
    }

    /// <inheritdoc/>
    public void Stop(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!mWorkerThreadInfos.ContainsKey(name))
            return;

        if (mWorkerThreadInfos.TryRemove(name, out var worker))
            worker.Source.Cancel();
    }

    /// <inheritdoc/>
    public RunningWorker[] GetRunningWorkers() =>
        mWorkerThreadInfos
            .Select(x => new RunningWorker
            {
                IsRunning = x.Value.IsAlive,
                Name = x.Key,
                Type = WorkerTypes[x.Value.WorkerType],
                Worker = x.Value.Type
            })
            .ToArray();

    /// <inheritdoc/>
    public event Func<string, Task> OnStart;

    /// <inheritdoc/>
    public event Func<string, Task> OnStop;

    /// <inheritdoc/>
    public event Func<string, Exception, Task> OnError;

    /// <inheritdoc/>
    public int GetAllWorkerCount() => WorkerInfoHandler.GlobalWorkersCount.Values.Sum();

    /// <inheritdoc/>
    public int GetAllWorkerCount(string workerName) => 
        !WorkerInfoHandler.GlobalWorkersCount.TryGetValue(workerName, out var count) ? 0 : count;

    private class WorkerInfo
    {
        public bool IsAlive { get; set; }

        public string Type { get; init; }

        public WorkerType WorkerType { get; init; }

        public CancellationTokenSource Source { get; init; }
    }
}