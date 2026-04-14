namespace AGame.Actors;

internal static class RpcTopics
{
    // --- NOTIFICATION TOPICS ---
    
    public const string CREATE_EVENT = "actor.event.create";
    public const string DESTROY_EVENT = "actor.event.destroy";
    public const string ACTOR_EVENT = "actor.event.user";
    public const string ALL_EVENTS = "actor.event.*";
    
    // --- SYSTEM ACTION TOPICS ---
    
    public const string ACTOR_COUNT = "actor.count";
    public const string ACTOR_CREATE = "actor.create";
    public const string ACTOR_DESTROY = "actor.destroy";
    public const string ACTOR_MOVE = "actor.move";
    public const string ACTOR_RPC = "actor.rpc";

    public const string ACTOR_REPLICATION = "actor.replication";
    
    public const string ACTOR_COMPONENT = "actor.component";
}