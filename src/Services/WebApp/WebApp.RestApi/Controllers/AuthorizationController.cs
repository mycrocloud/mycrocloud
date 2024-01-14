using Microsoft.AspNetCore.Mvc;
using WebApp.Api.Models;

namespace WebApp.RestApi.Controllers;

[Route("apps/{appId:int}/authorization")]
public class AuthorizationController : BaseController
{
    [HttpGet("policies")]
    public async Task<IActionResult> PolicyIndex(int appId)
    {
        return Ok();
    }

    [HttpGet("policies/create")]
    public async Task<IActionResult> PolicyCreate(int appId)
    {
        return Ok();
    }

    [HttpPost("policies/create")]
    public async Task<IActionResult> PolicyCreate(int appId, PolicySaveModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        return Ok();
    }

    [HttpGet("policies/{policyId:int}/edit")]
    public async Task<IActionResult> PolicyEdit(int appId, int policyId)
    {
        return Ok();
    }

    [HttpPost("policies/{policyId:int}/edit")]
    public async Task<IActionResult> PolicyEdit(int appId, int policyId, PolicySaveModel model)
    {
        return Ok();
    }

}
