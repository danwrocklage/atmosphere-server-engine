using System.Reflection;
using ACore.Abstractions;
using ACore.Abstractions.Telemetry;
using ACore.Configuration;
using ACore.Logging;
using ACore.Transport;
using AGame.Frontend;
using AGame.Transform;
using AUtils.IoC;
using Fb.Frontend.Bot;
using Fb.Frontend.Character;

IContainer fContainer()
{
    var builder = new ContainerBuilder();
    builder.Register(x => x
        .For<JsonFileConfigurationProvider>()
        .As<IConfigurationProvider>()
        .Add(() => "configuration.json"));
    ACore.Modules.Module module = new ConfigurationModule();
    module.ConfigureServices(builder);
    module = new LoggingModule();
    module.ConfigureServices(builder);
    builder.Singleton<BotMetrics, ICellMetrics>();
    builder.Singleton<BotEnvironment, ICellEnvironment>();

    module = new TransportModule();
    module.ConfigureServices(builder);
    builder.Transient<ConnectionPipeline>();

    builder.Transient<PublicApiClient>();
    builder.Transient<NetworkClient>();

    // Just for initialize static constructor
    new ActorTransformModule();
    
    builder.RegisterBy(typeof(PipelineHandler<>), RegisterMode.AsSelf, true);

    return builder.Build();
}

ConnectionPipeline.Initialize(Assembly.GetExecutingAssembly());

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

var container = fContainer();

using var publicApiClient = container.Resolve<PublicApiClient>();
await publicApiClient.Login(cts.Token);
var gameToken = await publicApiClient.GetGameToken(cts.Token);
if (string.IsNullOrEmpty(gameToken))
    return;

using var networkClient = container.Resolve<NetworkClient>();
await networkClient.Connect(cts.Token);
await networkClient.RunPipeline(gameToken, new GetCharactersDto(), cts.Token);