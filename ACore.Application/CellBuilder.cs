using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Telemetry;
using ACore.Abstractions.Worker;
using ACore.Application.Cluster;
using ACore.Application.Commands;
using ACore.Application.Configuration;
using ACore.Application.Telemetry;
using ACore.Application.Workers;
using AUtils.IoC;
using IConfigurationProvider = ACore.Application.Configuration.IConfigurationProvider;
using Module = ACore.Modules.Module;

namespace ACore.Application;

public class CellBuilder
{
    private readonly ContainerBuilder mContainerBuilder = new();
    private Module[] mModules = Array.Empty<Module>();

    private CellBuilder() { }
    
    public static async Task<CellBuilder> Create(CancellationToken token = default)
    {
        DebugLogger.WriteLine($"[{DateTime.Now:u}] Start creating cell application builder...");
        
        var systemArgs = Environment.GetCommandLineArgs().Skip(1).TakeWhile(x => x != "::").ToArray();

        var provider = IConfigurationProvider.Create(systemArgs);
        var configuration = await provider.Get(token);

        var builder = new CellBuilder();

        builder.mContainerBuilder.Register(x =>
            x.For<CellEnvironment>().As<ICellEnvironment>().Add(() => configuration).Singleton());
        
        LoadModuleAssemblies(configuration);
        CreateModuleObjects(builder);
        
        if(!string.IsNullOrEmpty(configuration.JsonPayload))
            builder.mContainerBuilder.Register(x => x
                .For<ACore.Configuration.Providers.JsonConfigurationProvider>()
                .As<Abstractions.IConfigurationProvider>()
                .Add(() => configuration.JsonPayload));

        DebugLogger.WriteLine("Creating cell application builder complete", ConsoleColor.Green);
        return builder;
    }

    public ICellHost Build()
    {
        var canUseCluster = mContainerBuilder.IsRegistered<IRpc>();
        if(canUseCluster)
        {
            mContainerBuilder.Register(x => x.For<CellCluster>().AsSelf().As<ICellCluster>().Singleton());
            mContainerBuilder.Transient<PingHandler, PingHandler, IRpcHandler<PingMessage>>();
            mContainerBuilder.Transient<WorkerInfoHandler, IRpcHandler<WorkerEvent>>();
            mContainerBuilder.Transient<CellInfoHandler, CellInfoHandler, IRpcHandler<CellInfoRequest>>();
            mContainerBuilder.Transient<CellPingWorker>();
            mContainerBuilder.Transient<CellWatchWorker>();
        }
        else
            DebugLogger.WriteLine("Rpc services are not registered. Cluster feature is disabled", ConsoleColor.Yellow);

        mContainerBuilder.Register(x =>
        {
            x.For<CellWorkersService>().As<ICellWorkers>().Singleton();
            if (!mContainerBuilder.IsRegistered<IRpc>())
                x.Add<IRpc>(() => null);
        });
        mContainerBuilder.Transient<CellWorkersMetrics, IInitializable>();
        mContainerBuilder.Singleton<PrometheusCellMetrics, ICellMetrics>();

        mContainerBuilder.Singleton<HttpCommandListener>();
        mContainerBuilder.RegisterBy<ICommandHandler>(RegisterMode.AsTarget);
        
        var container = mContainerBuilder.Build();
        var app = new CellApplication(container, mModules, canUseCluster);
        
        DebugLogger.WriteLine("Cell application has been built success", ConsoleColor.Green);
        return app;
    }

    #region Module Assemblies & Reflection

    private static void CreateModuleObjects(CellBuilder builder)
    {
        builder.mModules = Modules.Modules.Create();
        foreach (var module in builder.mModules)
            module.ConfigureServices(builder.mContainerBuilder);
    }

    private static void LoadModuleAssemblies(CellBuildConfiguration configuration)
    {
        DebugLogger.WriteLine("Load modules assemblies...");

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

        var loadedAssemblyFiles = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic)
            .Select(x => x.GetName())
            .ToHashSet();

        var foundModuleAssemblyFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.Module.dll");
        var requiredModulesNames = configuration.Modules.Select(x => $"{x}.Module")
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        var toLoad = foundModuleAssemblyFiles
            .Select(x => AssemblyName.GetAssemblyName(x))
            .Where(x => !loadedAssemblyFiles.Contains(x))
            .Where(x => requiredModulesNames.Any(r => x.FullName.StartsWith(r)))
            .ToArray();

        LoadAssembly(toLoad, loadedAssemblyFiles);

        DebugLogger.WriteLine("Assemblies is loaded", ConsoleColor.Green);
        
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
    }

    private static void LoadAssembly(AssemblyName[] files, HashSet<AssemblyName> loadedAssemblyFiles)
    {
        foreach (var toLoadAssembly in files)
        {
            if (loadedAssemblyFiles.Contains(toLoadAssembly))
                continue;

            var assembly = AppDomain.CurrentDomain.Load(toLoadAssembly);
            loadedAssemblyFiles.Add(toLoadAssembly);
            var refs = assembly.GetReferencedAssemblies();
            var toLoad = refs
                .Where(x => !loadedAssemblyFiles.Contains(x) && !x.FullName.StartsWith("System"))
                .ToArray();
            
            LoadAssembly(toLoad, loadedAssemblyFiles);
        }
    }

    private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs eventArgs)
    {
        DebugLogger.WriteLine($"Assembly was loaded: {eventArgs.LoadedAssembly.FullName}");
    }

    #endregion
}