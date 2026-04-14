using ACore.Abstractions;
using ACore.Abstractions.Extensions;
using ACore.Worker.Web.Routing.Attributes;

namespace ACore.Worker.Web.Routing.Info;

[RoutePrefix("web-routing")]
internal class RouteInfoController : Controller
{
    private readonly RouteManager mRouteManager;
    private RouteInfoViewModel[] mRoutes;
    private readonly string mCaption;

    public RouteInfoController(RouteManager routeManager, ICellEnvironment cellEnvironment)
    {
        mCaption = $"{cellEnvironment.Role.Capitalize()} HTTP Api ({cellEnvironment.Configuration.Capitalize()})";
        mRouteManager = routeManager;
    }

    /// <summary>
    /// Get endpoints browser UI
    /// </summary>
    [Get]
    public async Task Index()
    {
        Context.Response.StatusCode = 200;
        Context.Response.Headers["content-type"] = "text/html";
        await using var file = File.Open("./Routing/Info/custom-swagger.html", FileMode.Open);
        await file.CopyToAsync(Context.Response.OutputStream);
    }

    /// <summary>
    /// Get endpoints to show in browser UI
    /// </summary>
    [Get("routes")]
    public Task GetRoutes()
    {
        mRoutes ??= mRouteManager.RouteInfos.Select(x => new RouteInfoViewModel(x)).ToArray();
        return Response(mRoutes);
    }

    /// <summary>
    /// Get information about this api in browser UI
    /// </summary>
    [Get("caption")]
    public Task GetCaption() => Response(mCaption);
}