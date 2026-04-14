using System.Diagnostics;
using ACore.Abstractions;
using ACore.Abstractions.Logging;
using ACore.Abstractions.Worker;

namespace AGame.Actors;

[Worker("actor-tick")]
internal class ActorTickWorker : IRunnable
{
    private static readonly long sFps = TimeSpan.FromMilliseconds(1000d / 60d).Ticks;
    
    private readonly ILogger<ActorTickWorker> mLogger;
    private readonly ActorContainer mActorContainer;

    public ActorTickWorker(ILogger<ActorTickWorker> logger, ActorContainer actorContainer)
    {
        mLogger = logger;
        mActorContainer = actorContainer;
    }

    public async Task Run(CancellationToken token)
    {
        mLogger.Info("Start actor's ticking");
        Actor currentActor = null;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var last = Stopwatch.GetTimestamp();
                do
                {
                    var current = Stopwatch.GetTimestamp();
                    var delta = current - last;
                        
                    foreach (var actor in mActorContainer.Actors)
                    {
                        currentActor = actor;
                        var deltaSpan = TimeSpan.FromTicks(delta);
                        actor.InternalTick(deltaSpan);
                    }
            
                    mActorContainer.CommitRemove();

                    if (delta < sFps)
                    {
                        await Task.Delay(TimeSpan.FromTicks(sFps - delta));
                    }
                
                    last = current;
                } while (!token.IsCancellationRequested);
            }
            catch (Exception e)
            {
                mLogger.Error($"Tick failed on {currentActor}", e);
            }
        }
        mLogger.Info("Stop actor's ticking");

        await mActorContainer.StoreActors();
    }
}