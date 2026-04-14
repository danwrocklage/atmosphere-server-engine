using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using ACore.Abstractions.Worker;
using ACore.Modules;
using AUtils.IoC;

namespace AGame.Frontend.Queue;

public class FrontendQueueModule : ACore.Modules.Module
{
    public override void ConfigureServices(ContainerBuilder builder)
    {
        builder.Singleton<ConnectionAccounter, ConnectionAccounter, IConnectionAccounter>();
        builder.Singleton<ConnectionManager, ConnectionManager, IConnectionManager>();

        builder.Transient<ConfigurationHandler, IRpcHandler<ConfigurationRequest>>();
        builder.Transient<ConnectionReserveHandler, IRpcHandler<ConnectionReserveEvent>>();
        builder.Transient<ConnectionStatusHandler, IRpcHandler<ConnectionStatusEvent>>();

        builder.Transient<RemoveExpiredReservationsWorker>();
    }
    
    [RoleAny(Cell.FRONTEND)]
    public Task RunFrontend(CancellationToken token = default)
    {
        Subscribe<ConfigurationRequest>($"connection.settings.{Cell.AppId}");
        Subscribe<ConnectionReserveEvent>($"connection.reserve.{Cell.AppId}");
            
        Worker<RemoveExpiredReservationsWorker>(token);

        return Task.CompletedTask;
    }

    [RoleExcept(Cell.FRONTEND)]
    public Task RunConsumer(CancellationToken token = default)
    {
        Services.Resolve<ICellCluster>().CellFound += async role =>
        {
            if (role.Role != Cell.FRONTEND)
                return;

            var counter = Services.Resolve<ConnectionManager>();
            var settings = await Services.Resolve<IRpc>()
                .Call<ConfigurationRequest, ConfigurationEvent>(
                    $"connection.settings.{role.AppId}",
                    new ConfigurationRequest(), token);

            counter.Add(role.AppId, settings.MaxConnections);
        };
        Services.Resolve<ICellCluster>().CellLost += role =>
        {
            if (role.Role == Cell.FRONTEND)
                Services.Resolve<ConnectionManager>().Remove(role.AppId);

            return Task.CompletedTask;
        };
        Subscribe<ConnectionStatusEvent>();
        return Task.CompletedTask;
    }
}