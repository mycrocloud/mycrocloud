using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MycroCloud.WebMvc.Areas.Services.Models.WebApps;
using MycroCloud.WebMvc.Areas.Services.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MycroCloud.WebMvc.Areas.Services.Controllers;

[Authorize]
[Route("webapps/{WebApplicationName}/authentications")]
public class WebAppAuthenticationsController: BaseServiceController
{
    private readonly IWebAppAuthenticationService _webAppAuthenticationService;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["WebAppName"] = context.ActionArguments["WebApplicationName"];
    }

    public WebAppAuthenticationsController(IWebAppAuthenticationService webAppAuthenticationService)
    {
        _webAppAuthenticationService = webAppAuthenticationService;
    }

    [HttpGet("Configurations")]
    public async Task<IActionResult> Configurations(int WebApplicationId)
    {
        var vm = await _webAppAuthenticationService.GetConfigurationsViewModel(WebApplicationId);
        return View("/Areas/Services/Views/WebApp/Authentication/Configuration.cshtml", vm);
    }

    [HttpPost("Configurations")]
    public async Task<IActionResult> Configurations(int WebApplicationId, AuthenticationConfigurationViewModel viewModel)
    {
        await _webAppAuthenticationService.SaveSettings(WebApplicationId, viewModel);
        return RedirectToAction(nameof(Configurations), Request.RouteValues);
    }

    [HttpGet("Schemes")]
    public async Task<IActionResult> SchemeList(string WebApplicationName)
    {
        var vm = await _webAppAuthenticationService.GetSchemeListViewModel(WebApplicationName);
        return View("/Areas/Services/Views/WebApp/Authentication/SchemeList.cshtml", vm);
    }

    [HttpGet("Schemes/JwtBearer/Create")]
    public async Task<IActionResult> CreateJwtBearerScheme(int WebApplicationId)
    {
        var model = await _webAppAuthenticationService.GetCreateJwtBearerSchemeModel(WebApplicationId);
        return View("/Areas/Services/Views/WebApp/Authentication/SaveJwtBearerScheme.cshtml", model);
    }

    [HttpPost("Schemes/JwtBearer/Create")]
    public async Task<IActionResult> CreateJwtBearerScheme(int WebApplicationId, string WebApplicationName, JwtBearerAuthenticationSchemeSaveViewModel model)
    {
        await _webAppAuthenticationService.AddJwtBearerScheme(WebApplicationId, model);
        return RedirectToAction(nameof(SchemeList), new { WebApplicationName });
    }

    [HttpGet("Schemes/JwtBearer/{SchemeId:int}/Edit")]
    public async Task<IActionResult> EditJwtBearerScheme(int WebApplicationId, int SchemeId)
    {
        var model = await _webAppAuthenticationService.GetEditJwtBearerSchemeModel(WebApplicationId, SchemeId);
        return View("/Areas/Services/Views/WebApp/Authentication/SaveJwtBearerScheme.cshtml", model);
    }

    [HttpGet("Schemes/ApiKey/Create")]
    public async Task<IActionResult> CreateApiKeyScheme(int WebApplicationId)
    {
        var model = await _webAppAuthenticationService.GetCreateJwtBearerSchemeModel(WebApplicationId);
        return View("/Areas/Services/Views/WebApp/Authentication/SaveJwtBearerScheme.cshtml", model);
    }

    [HttpPost("Schemes/ApiKey/Create")]
    public async Task<IActionResult> CreateApiKeyScheme(int WebApplicationId, string WebApplicationName, JwtBearerAuthenticationSchemeSaveViewModel model)
    {
        await _webAppAuthenticationService.AddJwtBearerScheme(WebApplicationId, model);
        return RedirectToAction(nameof(SchemeList), new { WebApplicationName });
    }

    [HttpGet("Schemes/ApiKey/{SchemeId:int}/Edit")]
    public async Task<IActionResult> EditApiKeyScheme(int WebApplicationId, int SchemeId)
    {
        var model = await _webAppAuthenticationService.GetEditJwtBearerSchemeModel(WebApplicationId, SchemeId);
        return View("/Areas/Services/Views/WebApp/Authentication/SaveJwtBearerScheme.cshtml", model);
    }

    [HttpPost("Schemes/ApiKey/{SchemeId:int}/Edit")]
    public async Task<IActionResult> EditApiKeyScheme(string WebApplicationName, int SchemeId, JwtBearerAuthenticationSchemeSaveViewModel model)
    {
        await _webAppAuthenticationService.EditJwtBearerScheme(SchemeId, model);
        return RedirectToAction(nameof(SchemeList), new { WebApplicationName });
    }
}
