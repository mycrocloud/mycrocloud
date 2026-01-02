using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Extensions;
using WebApp.Api.Models.UserSettings;
using WebApp.Api.Utils;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

[Route("[controller]")]
public class UserSettingsController(AppDbContext dbContext): BaseController
{
    [HttpPost("tokens")]
    public async Task<IActionResult> CreateToken(CreateApiTokenRequest request)
    {
        var token = new ApiToken
        {
            UserId = User.GetUserId(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            Token = TokenUtils.GenerateReadableToken("mc", 32)
        };
                
        await dbContext.ApiTokens.AddAsync(token);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            token.Name,
            token.Token,
            token.CreatedAt
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