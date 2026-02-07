using Api.Filters;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class AuthenticationsController(
    AppDbContext dbContext,
    IAppCacheInvalidator cacheInvalidator) : BaseController
{
    [HttpGet("schemes")]
    public async Task<IActionResult> ListSchemes(int appId)
    {
        var schemes = await dbContext.AuthenticationSchemes
            .Where(s => s.AppId == appId)
            .ToListAsync();
        return Ok(schemes.Select(s => new
        {
            s.Id,
            s.Name,
            Type = s.Type.ToString(),
            s.Enabled,
            s.CreatedAt,
            s.UpdatedAt
        }));
    }

    [HttpPost("schemes")]
    public async Task<IActionResult> CreateScheme(int appId, AuthenticationScheme scheme)
    {
        scheme.AppId = appId;
        await dbContext.AuthenticationSchemes.AddAsync(scheme);
        await dbContext.SaveChangesAsync();
        await cacheInvalidator.InvalidateByIdAsync(appId);
        return Created();
    }
    
    [HttpPut("schemes/{schemeId:int}")]
    public async Task<IActionResult> CreateScheme(int appId, int schemeId, AuthenticationScheme scheme)
    {
        var existingScheme = await dbContext.AuthenticationSchemes
            .Where(s => s.AppId == appId && s.Id == schemeId)
            .SingleOrDefaultAsync();
        if (existingScheme is null)
        {
            return NotFound();
        }
        existingScheme.Name = scheme.Name;
        existingScheme.Description = scheme.Description;
        existingScheme.Type = scheme.Type;
        existingScheme.OpenIdConnectAuthority = scheme.OpenIdConnectAuthority;
        existingScheme.OpenIdConnectAudience = scheme.OpenIdConnectAudience;
        dbContext.AuthenticationSchemes.Update(existingScheme);
        await dbContext.SaveChangesAsync();
        await cacheInvalidator.InvalidateByIdAsync(appId);
        return NoContent();
    }

    [HttpGet("schemes/{schemeId:int}")]
    public async Task<IActionResult> GetScheme(int appId, int schemeId)
    {
        var scheme = await dbContext.AuthenticationSchemes
            .Where(s => s.AppId == appId && s.Id == schemeId)
            .SingleOrDefaultAsync();
        if (scheme is null)
        {
            return NotFound();
        }
        return Ok(new
        {
            scheme.Id,
            scheme.Name,
            scheme.Description,
            Type = scheme.Type.ToString(),
            scheme.OpenIdConnectAuthority,
            scheme.OpenIdConnectAudience,
            scheme.Enabled,
            scheme.CreatedAt,
            scheme.UpdatedAt
        });
    }
    
    [HttpDelete("schemes/{schemeId:int}")]
    public async Task<IActionResult> DeleteScheme(int appId, int schemeId)
    {
        var scheme = await dbContext.AuthenticationSchemes
            .Where(s => s.AppId == appId && s.Id == schemeId)
            .SingleOrDefaultAsync();
        if (scheme is null || scheme.Enabled)
        {
            return BadRequest();
        }
        dbContext.AuthenticationSchemes.Remove(scheme);
        await dbContext.SaveChangesAsync();
        await cacheInvalidator.InvalidateByIdAsync(appId);
        return NoContent();
    }

    [HttpPost("schemes/settings")]
    public async Task<IActionResult> Settings(int appId, List<int> schemeIds)
    {
        var schemes = dbContext.AuthenticationSchemes
            .Where(s => s.AppId == appId);
        foreach (var scheme in schemes)
        {
            scheme.Enabled = false;
            scheme.Order = null;
        }
        
        var selectedSchemes = schemeIds
            .Select(schemeId => schemes.Single(s => s.Id == schemeId));
        var order = 0;
        foreach (var scheme in selectedSchemes)
        {
            scheme.Enabled = true;
            scheme.Order = order++;
        }
        dbContext.AuthenticationSchemes.UpdateRange(schemes);
        await dbContext.SaveChangesAsync();
        await cacheInvalidator.InvalidateByIdAsync(appId);
        return NoContent();
    }
}
