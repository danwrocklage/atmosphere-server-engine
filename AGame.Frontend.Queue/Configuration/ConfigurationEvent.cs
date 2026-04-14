using ACore.Abstractions.Rpc;
using AUtils.Sil;

namespace AGame.Frontend.Queue;

[Topic(RpcType.Request)]
[Sil(140)]
internal struct ConfigurationRequest { }

[Sil(141)]
internal struct ConfigurationEvent
{
    public int MaxConnections { get; set; }
}