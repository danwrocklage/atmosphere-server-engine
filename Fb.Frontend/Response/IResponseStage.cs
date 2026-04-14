namespace Fb.Frontend.Response;

public interface IResponseStage
{
    Task<object> Execute(PlayerSession session, CancellationToken token = default);
}