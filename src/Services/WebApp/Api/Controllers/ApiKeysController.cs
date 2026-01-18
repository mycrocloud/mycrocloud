using System.ComponentModel.DataAnnotations;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class ApiKeysController(AppDbContext appDbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
        
        var apiKeys = await appDbContext.ApiKeys
            .Where(k => k.App == app)
            .ToListAsync();
        
        return Ok(apiKeys.Select(k => new
        {
            k.Id,
            k.Name,
            k.Key,
            k.CreatedAt,
            k.UpdatedAt
        }));
    }
    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetKey(int appId, int id)
    {
        var key = await appDbContext.ApiKeys.SingleAsync(k => k.App.Id == appId && k.Id == id);
        
        return Ok(new
        {
            key.Id,
            key.Name,
            key.Key,
            key.Metadata,
            key.CreatedAt,
            key.UpdatedAt
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(int appId, CreateUpdateRequest createRequest)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
        var entity = new ApiKey
        {
            App = app,
            Name = createRequest.Name,
            Key = createRequest.Key,
            Metadata = createRequest.Metadata
        };
        await appDbContext.ApiKeys.AddAsync(entity);
        await appDbContext.SaveChangesAsync();
        
        return Created();
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int appId, int id, CreateUpdateRequest updateRequest)
    {
        var key = await appDbContext.ApiKeys.SingleAsync(k => k.App.Id == appId && k.Id == id);
        key.Name = updateRequest.Name;
        key.Key = updateRequest.Key;
        key.Metadata = updateRequest.Metadata;
        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Edit(int appId, int id)
    {
        var key = await appDbContext.ApiKeys.SingleAsync(k => k.App.Id == appId && k.Id == id);
        appDbContext.ApiKeys.Remove(key);
        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }
}

public class CreateUpdateRequest
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public string Key { get; set; }

    public string? Metadata { get; set; }
}