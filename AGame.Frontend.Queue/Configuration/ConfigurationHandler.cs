using ACore.Abstractions;
using ACore.Abstractions.Rpc;

namespace AGame.Frontend.Queue;

/// <summary>
/// Send back a frontend connections counts configuration
/// </summary>
internal class ConfigurationHandler : IRpcHandler<ConfigurationRequest>
{
    private readonly Configuration mConfiguration;

    public ConfigurationHandler(IConfiguration configuration)
    {
        mConfiguration = configuration
            .Get(() => Configuration.Default);
    }

    public Task Handle(IRpcContext<ConfigurationRequest> context, CancellationToken token = default)
    {
        context.Reply(new ConfigurationEvent
        {
            MaxConnections = mConfiguration.MaxConnections
        });
        return Task.CompletedTask;
    }
}