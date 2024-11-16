using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Infrastructure;
using Route = WebApp.Domain.Entities.Route;
namespace WebApp.Admin.Pages.Routes
{
    public class ListModel(AppDbContext appDbContext) : PageModel
    {
        public App App { get; set; } = null!;
        public IEnumerable<Route> Routes { get; set; } = new List<Route>();
        public async Task OnGet(int appId)
        {
            App = await appDbContext.Apps.SingleAsync(x => x.Id == appId);
            Routes = await appDbContext.Routes
                        .Where(r => r.AppId == appId)
                        .OrderBy(r => r.CreatedAt)
                        .ToListAsync();
        }
        public async Task OnPostChangeStatus(int id, RouteStatus status)
        {
            var route = await appDbContext.Routes.FirstOrDefaultAsync(x => x.Id == id);
            if (route != null)
            {
                route.Status = status;
                await appDbContext.SaveChangesAsync();
            }
        }
    }
}
