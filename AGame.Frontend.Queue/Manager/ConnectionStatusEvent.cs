using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Frontend.Queue;

[Topic("connection.event")]
[Sil(142)]
public struct ConnectionStatusEvent
{
    public Guid AppId { get; set; }

    public bool IsConnecting { get; set; }
}