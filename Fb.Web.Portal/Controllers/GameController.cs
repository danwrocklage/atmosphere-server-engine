using System.ComponentModel;
using ACore.Abstractions;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Account;
using AGame.Core.Identity;
using AGame.Frontend.Queue;

namespace Fb.Web.Portal.Controllers;

/// <summary>
/// Controller for frontend servers scheduling 
/// </summary>
public class GameController : BasePortalController
{
    private readonly ICellCluster mCluster;
    private readonly IConnectionManager mConnectionManager;
    private readonly IJwtService mJwtService;

    public GameController(IJwtService jwtService, IConnectionManager connectionManager, ICellCluster cluster)
    {
        mJwtService = jwtService;
        mConnectionManager = connectionManager;
        mCluster = cluster;
    }

    /// <summary>
    /// Queue current account for connection for game server
    /// </summary>
    [Post("prepare")]
    [Description("Queue account for game server")]
    public async Task PrepareForConnection()
    {
        var reservationCellId = await mConnectionManager.ReserveConnection(AccountId);
        if (!reservationCellId.HasValue)
        {
            Response(406);
            return;
        }

        var claims = JwtServiceExtensions
            .GetClaimsByEntity((AccountId, typeof(AccountEntity).FullName, GrandTypes.Client));

        var gameToken = mJwtService.Generate(claims, out var expires);
        await Response(new
        {
            Token = gameToken,
            Expires = expires,
            Endpoint = mCluster.GetRoleEndpoint(reservationCellId.Value)
        });
    }
}