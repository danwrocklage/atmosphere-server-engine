using System.Runtime.CompilerServices;
using ACore.Abstractions;
using ACore.Configuration.Providers;
using ACore.Modules;
using AUtils.IoC;

[assembly:InternalsVisibleTo("ACore.Application")]
[assembly:InternalsVisibleTo("ACore.Configuration.Tests")]

namespace ACore.Configuration;

public class ConfigurationModule : Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<Configuration, IConfiguration, IConfigurationManager>();
        builder.Transient<ArgsConfigurationProvider, IConfigurationProvider>();
        builder.Transient<EnvVarConfigurationProvider, IConfigurationProvider>();
    }
}