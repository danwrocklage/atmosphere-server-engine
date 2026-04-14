using System.Runtime;
using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;
using ACore.Application.Commands;
using ACore.Application.Workers;
using ACore.Modules;
using AUtils.IoC;
using Sentry;

namespace ACore.Application;

/// <summary>
/// Cell host application implementation
/// </summary>
internal class CellApplication : ICellHost
{
    private readonly Module[] mModules;
    private readonly bool mCanUseCluster;
    private CancellationTokenSource mCancellationTokenSource;
    private readonly ILogger mLogger;
    private IDisposable mSentrySdk;

    public CellApplication(IContainer services, Module[] modules, bool canUseCluster)
    {
        mModules = modules;
        mCanUseCluster = canUseCluster;
        Services = services;
        mLogger = Services.Resolve<ILogger>();
    }

    public IContainer Services { get; }

    public async Task Run(CancellationToken token)
    {
        mCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        ShowCellInfo();

        InitSentry();
        SetGlobalHandlers();
        await RunObjectsInitializers();
        
        try
        {
            Services.Resolve<IRpcSubscribe>().Subscribe<WorkerEvent>();
        }
        catch (Exception)
        {
            mLogger.Debug("Cell", "Failed to subscribe on worker events");
        }
        
        RunClusterSupply();
        await StartModules();
        
        await Services.Resolve<HttpCommandListener>()
            .Run(mCancellationTokenSource.Token);
    }

    private async Task StartModules()
    {
        foreach (var module in mModules)
            await module.Run(Services, mCancellationTokenSource.Token);
    }

    private async Task RunObjectsInitializers()
    {
        var initializers = Services.Resolve<IInitializable[]>();
        foreach (var initializable in initializers)
            initializable.Initialize();
        
        var asyncInitializers = Services.Resolve<IAsyncInitializable[]>();
        foreach (var initializable in asyncInitializers)
            await initializable.InitializeAsync();
    }

    private void RunClusterSupply()
    {
        // We can't use attribute for index (we don't have a ref to Sil from Abstractions)
        AUtils.Sil.Sil.Register<CellInfo>(101);
        AUtils.Sil.Sil.Register<CellError>(102);
        AUtils.Sil.Sil.Register<GlobalNotificationEvent>(109);
        
        if(!mCanUseCluster)
            return;
        
        Services.Resolve<ICellWorkers>().Run("cell-pinger", "cell-ping", mCancellationTokenSource.Token, 1);
        Services.Resolve<ICellWorkers>().Run("cell-watcher", "cell-watch", mCancellationTokenSource.Token, 1);
    }

    private void SetGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        {
            mLogger.Debug("Cell", $"{nameof(AppDomain)}.{nameof(AppDomain.UnhandledException)}");
            OnExit((Exception) a.ExceptionObject);
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            mLogger.Debug("Cell",$"{nameof(AppDomain)}.{nameof(AppDomain.ProcessExit)}");
            OnExit();
        };
        Console.CancelKeyPress += (_, _) =>
        {
            mLogger.Warn("Cell","Console canceling");
            OnExit();
        };
    }
    
    private void InitSentry()
    {
        var sentryDsn = Services.Resolve<IConfiguration>().Get<string>("sentry", () => null);
        if (string.IsNullOrEmpty(sentryDsn)) 
            return;
        
        mSentrySdk = SentrySdk.Init(o =>
        {
            var env = Services.Resolve<ICellEnvironment>();
            var isDevelopment =
                string.Compare(env.Configuration, "Development", StringComparison.InvariantCultureIgnoreCase) == 0;

            o.Dsn = sentryDsn;
            o.Release = env.Build;
            o.Debug = isDevelopment;
            o.Environment = env.Configuration;
            o.DefaultTags.Add("role", env.Role);
            o.DiagnosticLogger = 
                new SentryLoggingAdapter(Services.Resolve<ILogger<SentryLoggingAdapter>>());

            // Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
            o.TracesSampleRate = 1.0;
        });
    }

    private void OnExit(Exception ex = null)
    {   
        mCancellationTokenSource.Cancel();

        // Try to send exception to monitoring systems
        if (ex != null)
        {
            var env = Services.Resolve<ICellEnvironment>();
            Services.Resolve<IRpc>()?
                .Call(RpcTopics.ERROR, new CellError
                {
                    AppId = Cell.AppId,
                    Message = ex.GetFullMessage(),
                    Timestamp = DateTime.UtcNow,
                    Info = new CellInfo
                    {
                        Build = env.Build,
                        Configuration = env.Configuration,
                        Endpoint = env.Endpoint,
                        Role = env.Role
                    }
                }, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }

    private void ShowCellInfo()
    {
        var environment = Services.Resolve<ICellEnvironment>();
        mLogger.Log(environment.ToString());
        mLogger.Log( environment.IsContainerBuild ? "Containerized" : $"PID: {Environment.ProcessId}");

#if Production
        mLogger.Log("Production");
#elif Staging
        mLogger.Log("Staging");
#else
        mLogger.Log("Development");
#endif

        mLogger.Log($"CG mode: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
        mLogger.Log($".NET: {Environment.Version}");
        mLogger.Log("");
    }

    public async ValueTask DisposeAsync()
    {
        // Dispose modules in reverse order
        for (int i = mModules.Length - 1; i >= 0; i--)
        {
            switch (mModules[i])
            {
                // ReSharper disable SuspiciousTypeConversion.Global
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
                // ReSharper restore SuspiciousTypeConversion.Global
            }
        }
        
        if (Services != null)
            await Services.DisposeAsync();
        
        mSentrySdk?.Dispose();
    }
}