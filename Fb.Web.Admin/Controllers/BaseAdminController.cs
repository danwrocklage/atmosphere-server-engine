using ACore.Worker.Web;
using ACore.Worker.Web.Routing;

namespace Fb.Web.Admin.Controllers;

public abstract class BaseAdminController : Controller
{
    protected Guid StaffId => Guid.Parse(Session.GetEntityId());
}