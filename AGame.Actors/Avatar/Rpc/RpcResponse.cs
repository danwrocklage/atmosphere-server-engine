using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(119)]
internal class RpcResponse
{
    public bool IsSuccess { get; set; }
    
    public object Reply { get; set; }
}