using AUtils.Sil;

namespace AGame.Actors.Eventing;

[Sil(114)]
internal class ActorEvent
{
    public Guid? SenderActorId { get; set; }
    
    public ActorEventType Type { get; set; }
    
    public object Payload { get; set; }
    
    public Guid[] TargetActorIds { get; set; }
}