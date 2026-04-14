using AUtils.Sil;

namespace AGame.Actors.Avatar;

[Sil(111)]
internal struct CreateActorResponse
{
    public bool IsSuccess { get; set; }
    
    public Guid ActorId { get; set; }
    
    public Guid CellId { get; set; }
}