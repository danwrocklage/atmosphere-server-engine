using System.ComponentModel;
using ACore.Worker.Web.Routing;
using ACore.Worker.Web.Routing.Attributes;
using AGame.Core.Journal;
using Fb.Web.Admin.Models;

namespace Fb.Web.Admin.Controllers;

[RoutePrefix("journal")]
[Role("journal")]
public class JournalController : Controller
{
    private readonly IJournalService mJournalService;

    public JournalController(IJournalService journalService)
    {
        mJournalService = journalService;
    }

    [Get]
    [Description("Get all journal entries filtered by category")]
    public async Task GetByCategory([FromQuery] string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            Response(400);
            return;
        }

        var journals = await mJournalService.GetByCategory(category);

        var result = journals
            .Select(x => new JournalResponse
            {
                Category = x.Category,
                Id = x.Id,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                Links = x.Links.Select(l => new Models.JournalLink { Id = l.Id, Type = l.Type }).ToArray()
            }).ToArray();
        
        await Response(result);
    }
}