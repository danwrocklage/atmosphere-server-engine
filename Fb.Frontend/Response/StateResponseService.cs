using System.Collections.Concurrent;
using AUtils.IoC;

namespace Fb.Frontend.Response;

public class StateResponseService
{
    private static readonly ConcurrentDictionary<Guid, PlayerSession> sSessions = new();

    private readonly IResponseStage[] mStages;
    private readonly IContainer mContainer;

    public StateResponseService(IEnumerable<IResponseStage> stages, IContainer container)
    {
        mContainer = container;
        mStages = stages.ToArray();
    }

    public async Task<ResponseStateDto> Process(Guid entityId, CancellationToken token = default)
    {
        if (!sSessions.TryGetValue(entityId, out var session))
        {
            session = mContainer.Resolve<PlayerSession>();
            session.AccountId = entityId;

            sSessions.TryAdd(entityId, session);
        }
        
        var state = new ResponseStateDto();
        foreach (var stage in mStages)
            await stage.Execute(session, token);

        return state;
    }
}