using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure;
using Route = WebApp.Domain.Entities.Route;

namespace WebApp.Admin.Pages.Routes;

public class EditModel(AppDbContext appDbContext) : PageModel
{
    [BindProperty] public Route Route { get; set; } = default!;

    public async Task OnGet(int appId, int routeId)
    {
        Route = await appDbContext.Routes.SingleAsync(r => r.AppId == appId && r.Id == routeId);
    }
}