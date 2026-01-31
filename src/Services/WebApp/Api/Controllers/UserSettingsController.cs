using Api.Models.UserSettings;
using Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Extensions;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace Api.Controllers;

[Route("[controller]")]
public class UserSettingsController(AppDbContext dbContext): BaseController
{
    [HttpPost("tokens")]
    public async Task<IActionResult> CreateToken(CreateApiTokenRequest request)
    {
        var plainToken = TokenUtils.GenerateReadableToken("mcp", 32);
        var apiToken = new ApiToken
        {
            UserId = User.GetUserId(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            HashedToken = TokenUtils.HashToken(plainToken)
        };

        await dbContext.ApiTokens.AddAsync(apiToken);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            apiToken.Name,
            Token = plainToken,
            apiToken.CreatedAt
        });
    }
    
    [HttpGet("tokens")]
    public async Task<IActionResult> ListToken()
    {
        var tokens = await dbContext.ApiTokens
            .Where(t => t.UserId == User.GetUserId())
            .ToListAsync();

        return Ok(tokens.Select(t => new
        {
            t.Id,
            t.Name,
            Status = t.Status.ToString(),
            t.CreatedAt
        }));
    }

    [HttpGet("tokens/{id:int}")]
    public async Task<IActionResult> GetToken(int id)
    {
        var token = await dbContext.ApiTokens
            .Where(t => t.UserId == User.GetUserId() && t.Id == id)
            .FirstOrDefaultAsync();

        if (token is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            token.Id,
            token.Name,
            Status = token.Status.ToString(),
            token.CreatedAt
        });
    }

    [HttpPost("tokens/{id:int}/regenerate")]
    public async Task<IActionResult> RegenerateToken(int id)
    {
        var token = await dbContext.ApiTokens
            .Where(t => t.UserId == User.GetUserId() && t.Id == id)
            .FirstOrDefaultAsync();

        if (token is null)
        {
            return NotFound();
        }

        var plainToken = TokenUtils.GenerateReadableToken("mcp", 32);
        token.HashedToken = TokenUtils.HashToken(plainToken);
        token.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            token.Id,
            token.Name,
            Token = plainToken
        });
    }

    [HttpDelete("tokens/{id:int}")]
    public async Task<IActionResult> DeleteToken(int id)
    {
        await dbContext.ApiTokens
            .Where(t => t.UserId == User.GetUserId() && t.Id == id)
            .ExecuteDeleteAsync();

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}