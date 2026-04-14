using ACore.Worker.Web;
using ACore.Worker.Web.Routing;

namespace Fb.Web.Portal.Controllers;

public abstract class BasePortalController : Controller
{
    protected Guid AccountId => Guid.Parse(Session.GetEntityId());
}