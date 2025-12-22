using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storages.Api.Helpers;
using Storages.Api.Models.Kv;
using Storages.Core.Services;

namespace Storages.Api.Controllers;

[Authorize]
[ApiController]
[Route("kv/instances/{instanceId:guid}/values/{*key}")]
public class KvValuesController(IKvValueService kvValueService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ReadValue(Guid instanceId, string key)
    {
        var kv = await kvValueService.ReadValue(User.GetUserId(), instanceId, key);

        return Ok(kv.Value);
    }
    
    [HttpPost]
    public async Task<IActionResult> WriteValue(Guid instanceId, string key, WriteKvValueRequest request)
    {
        await kvValueService.WriteValue(User.GetUserId(), instanceId, key, request.Value);
        
        return Ok();
    }
}