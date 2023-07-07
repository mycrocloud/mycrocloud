using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockServer.Web.Attributes;
using MockServer.Web.Filters;
using MockServer.Web.Models.WebApplications.Routes;
using MockServer.Web.Services;
using RouteName = MockServer.Web.Common.Constants.RouteName;

namespace MockServer.Web.Controllers;

[Authorize]
[Route("webapps/{WebApplicationName}/routes")]
[GetAuthUserWebApplicationId(RouteName.WebApplicationName, RouteName.WebApplicationId)]
public class WebApplicationRoutesController : Controller
{
    private readonly IWebApplicationRouteService _webApplicationRouteWebService;

    public WebApplicationRoutesController(IWebApplicationRouteService webApplicationRouteWebService)
    {
        _webApplicationRouteWebService = webApplicationRouteWebService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index(int WebApplicationId, string SearchTerm, string Sort)
    {
        var pm = await _webApplicationRouteWebService.GetIndexViewModel(WebApplicationId, SearchTerm, Sort);
        return View("/Views/WebApplications/Routes/Index.cshtml", pm);
    }

    [AjaxOnly]
    [HttpGet]
    public async Task<IActionResult> List(int WebApplicationId, string SearchTerm, string Sort)
    {
        var routes = await _webApplicationRouteWebService.GetList(WebApplicationId, SearchTerm, Sort);
        return Ok(routes.Select(r => new
        {
            r.RouteId,
            r.Name,
            r.Description
        }));
    }

    [AjaxOnly]
    [HttpGet("sample")]
    public IActionResult Sample()
    {
        var json = System.IO.File.ReadAllText("Views/WebApplications/Routes/_Partial/sample.json");
        var sampleRoute = JsonSerializer.Deserialize<dynamic>(json);
        return Ok(sampleRoute);
    }

    //[AjaxOnly]
    [HttpGet("downloadJson")]
    public IActionResult DownloadJson()
    {
        var json = System.IO.File.ReadAllText("Views/WebApplications/Routes/_Partial/sample.json");
        var byteArray = Encoding.ASCII.GetBytes(json);
        return File(byteArray, "application/json", "sample.json");
    }

    [AjaxOnly]
    [HttpGet("{RouteId:int}")]
    public async Task<IActionResult> Get(int RouteId)
    {
        var json = System.IO.File.ReadAllText("Views/WebApplications/Routes/_Partial/sample.json");
        var sampleRoute = JsonSerializer.Deserialize<dynamic>(json);
        return Ok(sampleRoute);
        //return ok(route);
    }

    [AjaxOnly]
    [HttpPost("create")]
    public async Task<IActionResult> Create(int WebApplicationId, [FromBody] RouteSaveModel route)
    {
        return Ok(route);
    }
    
    [AjaxOnly]
    [HttpPost("edit/{RouteId:int}")]
    public async Task<IActionResult> Edit(int RouteId, [FromBody] RouteSaveModel route)
    {
        // if (!await _webApplicationRouteWebService.ValidateEdit(RouteId, route, ModelState))
        // {
        //     var allErrors = ModelState.Values.SelectMany(v => v.Errors);
        //     return BadRequest(allErrors);
        // }
        // await _webApplicationRouteWebService.Edit(RouteId, route);
        return NoContent();
    }
    
    [AjaxOnly]
    [HttpPost("delete/{RouteId:int}")]
    public async Task<IActionResult> Delete(int RouteId)
    {
        await _webApplicationRouteWebService.Delete(RouteId);
        return Ok();
    }
}