using MycroCloud.WebMvc.Controllers;
using MycroCloud.WebMvc.Filters;
using MycroCloud.WebMvc.Areas.Services.Models.WebApps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MycroCloud.WebMvc.Areas.Services.Services;

namespace MycroCloud.WebMvc.Areas.Services.Controllers;

[Authorize]
[Route("webapps")]
//[GetAuthUserWebApplicationId(Constants.RouteName.WebAppName, Constants.RouteName.WebAppId)]
public class WebAppsController : BaseController
{
    private readonly IWebAppService _webAppService;

    public WebAppsController(IWebAppService webAppService)
    {
        _webAppService = webAppService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(WebAppSearchModel fm)
    {
        var vm = await _webAppService.GetIndexViewModel(fm);
        return View("/Areas/Services/Views/WebApp/Index.cshtml", vm);
    }

    [HttpGet("Create")]
    public Task<IActionResult> Create()
    {
        var vm = new WebAppCreateViewModel();
        return Task.FromResult<IActionResult>(View("/Areas/Services/Views/WebApp/Create.cshtml", vm));
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create(WebAppCreateViewModel app)
    {
        if (!ModelState.IsValid)
        {
            return View("/Areas/Services/Views/WebApp/Create.cshtml", app);
        }
        await _webAppService.Create(app);

        return RedirectToAction(nameof(View), new { WebApplicationName = app.Name });
    }

    [HttpGet("{WebApplicationName}")]
    //[HttpGet("{WebApplicationName}/View")]
    public async Task<IActionResult> View(int WebApplicationId)
    {
        var vm = await _webAppService.Get(WebApplicationId);
        return View("/Areas/Services/Views/WebApp/View.cshtml", vm);
    }
    
    [HttpPost("rename")]
    public async Task<IActionResult> Rename(int WebApplicationId, string newName)
    {
        await _webAppService.Rename(WebApplicationId, newName);
        return RedirectToAction(nameof(Index), new { WebApplicationName = newName });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(int WebApplicationId)
    {
        await _webAppService.Delete(WebApplicationId);
        return RedirectToAction(nameof(Index));
    }
}