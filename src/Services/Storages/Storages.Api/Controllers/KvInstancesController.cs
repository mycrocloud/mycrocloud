using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storages.Api.Helpers;
using Storages.Api.Models.Kv;
using Storages.Core.Services;

namespace Storages.Api.Controllers;

[Authorize]
[ApiController]
[Route("kv/instances")]
public class KvInstancesController(IKvService kvService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var instances = await kvService.ListInstances(User.GetUserId());

        return Ok(instances.Select(i => new
        {
            i.Id,
            i.Name
        }));
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateInstanceRequest request)
    {
        var entity = request.ToKvInstanceEntity();
        
        await kvService.CreateInstance(User.GetUserId(), entity);
        
        return Ok();
    }
    
    [HttpGet("{instanceId:guid}")]
    public async Task<IActionResult> Get(Guid instanceId)
    {
        var instance = await kvService.GetInstanceById(User.GetUserId(), instanceId);
        
        return Ok(new
        {
            instance.Id,
            instance.Name
        });
    }

    [HttpDelete("{instanceId:guid}")]
    public async Task<IActionResult> Delete(Guid instanceId)
    {
        await kvService.DeleteInstance(User.GetUserId(), instanceId);
        
        return Ok();
    }
}