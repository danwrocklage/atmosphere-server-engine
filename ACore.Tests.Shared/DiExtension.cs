using ACore.Abstractions;
using ACore.Abstractions.Database;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Telemetry;
using ACore.Tests.Shared.Database;
using AUtils.IoC;

namespace ACore.Tests.Shared;

public static class DiExtension
{
    public static void AddFakeServices(this ContainerBuilder builder)
    {
        builder.Transient<FakeLogger, ILogger>();
        builder.Transient<FakeConfiguration, IConfiguration>();
        builder.Transient<FakeMetrics, ICellMetrics>();
        builder.Transient<FakeEnvironment, ICellEnvironment>();
        builder.Singleton<FakeDatabase, FakeDatabase, IDatabase>();
        builder.Register(x => x.For(typeof(TypedFakeLogger<>)).As(typeof(ILogger<>)));
    }
}