using ACore.Abstractions;
using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace ACore.Application.Cluster;

[Sil(103)]
[Topic(RpcType.Request)]
internal struct CellInfoRequest { }

internal class CellInfoHandler : IRpcHandler<CellInfoRequest>
{
    private readonly ICellEnvironment mEnvironment;

    public CellInfoHandler(ICellEnvironment environment)
    {
        mEnvironment = environment;
    }

    public Task Handle(IRpcContext<CellInfoRequest> context, CancellationToken token = default)
    {
        if(token.IsCancellationRequested || !context.IsReplyRequired)
            return Task.CompletedTask;
        
        context.Reply(new CellInfo
        {
            AppId = Cell.AppId,
            Build = mEnvironment.Build,
            Configuration = mEnvironment.Configuration,
            Endpoint = mEnvironment.Endpoint,
            Role = mEnvironment.Role
        });
        return Task.CompletedTask;
    }
}