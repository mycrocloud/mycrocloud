using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MockServer.Web.Attributes;
using MockServer.Web.Models.Requests;
using MockServer.Web.Services.Interfaces;

namespace MockServer.Web.Controllers;

[Authorize]
[Route("projects/{projectName}/requests")]
public class RequestsController : Controller
{
    private readonly IRequestWebService _requestService;
    public RequestsController(IRequestWebService requestService)
    {
        _requestService = requestService;
    }

    [AjaxOnly]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Open(string projectName, int id)
    {
        ViewData["ProjectName"] = projectName;
        ViewData["RequestId"] = id;
        var vm = await _requestService.GetRequestOpenViewModel(projectName, id);
        return PartialView("Views/Requests/_RequestOpen.cshtml", vm);
    }

    [AjaxOnly]
    [HttpGet("create")]
    public async Task<IActionResult> GetCreatePartial(string projectName)
    {
        ViewData["ProjectName"] = projectName;
        var model = await _requestService.GetCreateRequestViewModel(projectName);
        return PartialView("Views/Requests/_CreateRequestPartial.cshtml", model);
    }

    [AjaxOnly]
    [HttpPost("create")]
    public async Task<IActionResult> CreateAjax(string projectName, CreateUpdateRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);
            return BadRequest(allErrors);
        }
        int id = await _requestService.Create(projectName, request);
        return Ok(new
        {
            id = id
        });
    }

    [AjaxOnly]
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> GetEditPartial(string projectName, int id)
    {
        var vm = await _requestService.GetGetCreateRequestViewModel(projectName, id);
        ViewData["FormMode"] = "Edit";
        return PartialView("Views/Requests/_CreateRequestPartial.cshtml", vm);
    }

    [AjaxOnly]
    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(string projectName, int id, CreateUpdateRequestViewModel request)
    {
        if (!await _requestService.ValidateEdit(projectName, id, request, ModelState))
        {
            IEnumerable<ModelError> allErrors = ModelState.Values.SelectMany(v => v.Errors);
            return BadRequest(allErrors);
        }
        await _requestService.Edit(projectName, id, request);
        return NoContent();
    }

    [AjaxOnly]
    [HttpGet("{id:int}/authorization")]
    public async Task<IActionResult> GetAuthorizationPartial(string projectName, int id)
    {
        var vm = await _requestService.GetAuthorizationConfigViewModel(projectName, id);
        return PartialView("Views/Requests/_AuthorizationConfigPartial.cshtml", vm);
    }

    [AjaxOnly]
    [HttpPost("{id:int}/authorization")]
    public async Task<IActionResult> ConfigAuthorization(string projectName, int id, AuthorizationConfigViewModel auth)
    {
        await _requestService.UpdateRequestAuthorizationConfig(projectName, id, auth);
        return NoContent();
    }

    [AjaxOnly]
    [HttpPost("{id:int}/config/fixed-request")]
    public async Task<IActionResult> ConfigFixedRequest(string projectName, int id, string[] fields, FixedRequestConfigViewModel config)
    {
        await _requestService.SaveFixedRequestConfig(projectName, id, fields, config);
        return Ok(config);
    }

    [AjaxOnly]
    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(string projectName, int id)
    {
        await _requestService.Delete(projectName, id);
        return Ok();
    }
}