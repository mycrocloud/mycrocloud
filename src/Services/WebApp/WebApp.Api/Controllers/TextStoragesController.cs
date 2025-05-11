using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class TextStoragesController(AppDbContext appDbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var storages = await appDbContext.TextStorages.Where(v => v.AppId == appId).ToListAsync();
        return Ok(storages.Select(storage => new
        {
            storage.Id,
            storage.Name,
            Size = !string.IsNullOrEmpty(storage.Content) ? Encoding.UTF8.GetBytes(storage.Content).Length : 0,
            storage.CreatedAt,
            storage.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Post(int appId, CreateUpdateTextStorageRequest createUpdateTextStorageRequest)
    {
        var entity = createUpdateTextStorageRequest.ToEntity();
        entity.AppId = appId;
        await appDbContext.TextStorages.AddAsync(entity);
        await appDbContext.SaveChangesAsync();
        return Created("", new { entity.Id });;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int appId, int id, CreateUpdateTextStorageRequest createUpdateTextStorageRequest)
    {
        var entity = await appDbContext.TextStorages.SingleAsync(v => v.AppId == appId && v.Id == id);
        createUpdateTextStorageRequest.CopyToEntity(entity);
        appDbContext.TextStorages.Update(entity);
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int appId, int id)
    {
        var storage = await appDbContext.TextStorages.SingleAsync(v => v.AppId == appId && v.Id == id);
        return Ok(new
        {
            storage.Id,
            storage.Name,
            storage.Description,
            Size = !string.IsNullOrEmpty(storage.Content) ? Encoding.UTF8.GetBytes(storage.Content).Length : 0,
            storage.CreatedAt,
            storage.UpdatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int appId, int id)
    {
        var storage = await appDbContext.TextStorages.SingleAsync(v => v.AppId == appId && v.Id == id);
        appDbContext.TextStorages.Remove(storage);
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/content")]
    public async Task<string> GetContent(int appId, int id)
    {
        var storage = await appDbContext.TextStorages.SingleAsync(v => v.AppId == appId && v.Id == id);
        return storage.Content;
    }

    [HttpPut("{id:int}/content")]
    [Consumes("text/plain")]
    public async Task<IActionResult> PutContent(int appId, int id, [FromBody] string content)
    {
        var storage = await appDbContext.TextStorages.SingleAsync(v => v.AppId == appId && v.Id == id);
        storage.Content = content;
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateUpdateTextStorageRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public TextStorage ToEntity()
    {
        return new()
        {
            Name = Name,
            Description = Description
        };
    }

    public void CopyToEntity(TextStorage variable)
    {
        variable.Name = Name;
        variable.Description = Description;
    }
}