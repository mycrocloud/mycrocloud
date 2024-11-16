using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Admin.Pages.Apps;

public class EditPageModel(AppDbContext appDbContext) : PageModel
{
    [BindProperty]
    public App App { get; set; } = default!;
        
    public async Task OnGet(int appId)
    {
        App = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
    }
    
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        appDbContext.Attach(App).State = EntityState.Modified;
        await appDbContext.SaveChangesAsync();
        
        return RedirectToPage("List");
    }

    public async Task<IActionResult> OnPostDelete(int appId)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
        appDbContext.Remove(app);
        await appDbContext.SaveChangesAsync();
        
        return RedirectToPage("List");
    }
}