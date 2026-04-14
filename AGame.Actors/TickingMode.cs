namespace AGame.Actors;

public enum TickingMode : byte
{
    NoTicking,
    ActorTickingOnly,
    ComponentsTickingOnly,
    AllTicking
}