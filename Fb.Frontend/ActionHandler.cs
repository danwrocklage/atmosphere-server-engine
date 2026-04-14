using AGame.Frontend;
using Fb.Frontend.Response;
using Fb.Mechanics;

namespace Fb.Frontend;

/// <summary>
/// Handle when player does some action (or doesn't and just views world around
/// </summary>
public class ActionHandler : PipelineHandler<ActionDto>
{
    private readonly PlayerSession mSession;
    private readonly StateResponseService mResponseService;
    
    public ActionHandler(StateResponseService responseService, PlayerSession session)
    {
        mResponseService = responseService;
        mSession = session;
    }

    protected override async Task<object> Handle(ActionDto message, PipelineHandlerContext context)
    {
        var transform = await (await mSession.CharacterAvatar())
            .Get<CharacterTransformComponent>();
        
        if (message.Direction.HasValue)
            await transform.Rpc(x => x
                .Look(message.Direction.Value), context.CancellationToken);

        if (message.Type.HasValue)
        {
            if(message.Type.Value == ActionType.Jump)
                await transform.Rpc(x => x.Jump(), context.CancellationToken);
            else
                await transform.Rpc(x => x.ActionType, ToCharacterAction(message.Type.Value), context.CancellationToken);
        }
    
        return await mResponseService.Process(context.EntityId, context.CancellationToken);
    }

    private static CharacterActionType ToCharacterAction(ActionType messageType) =>
        messageType switch
        {
            ActionType.Idle => CharacterActionType.Idle,
            ActionType.Walk => CharacterActionType.Walk,
            ActionType.Run => CharacterActionType.Run,
            ActionType.Sprint => CharacterActionType.Sprint,
            ActionType.Crouch => CharacterActionType.Crouch,
            ActionType.Jump => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };
}