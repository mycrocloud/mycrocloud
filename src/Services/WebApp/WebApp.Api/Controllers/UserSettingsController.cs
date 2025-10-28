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
        var token = new UserToken
        {
            UserId = User.GetUserId(),
            Name = request.Name,
            Provider = "MycroCloud",
            Purpose = UserTokenPurpose.ApiToken,
            CreatedAt = DateTime.UtcNow,
            Token = TokenUtils.GenerateReadableToken("mc", 32)
        };
                
        await dbContext.UserTokens.AddAsync(token);
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
        var tokens = await dbContext.UserTokens
            .Where(t => t.UserId == User.GetUserId() && t.Purpose == UserTokenPurpose.ApiToken)
            .ToListAsync();

        return Ok(tokens.Select(t => new
        {
            t.Id,
            t.Name,
            t.Token,
            Status = t.Status.ToString(),
            t.CreatedAt
        }));
    }
    
    [HttpPost("tokens/{id:int}/revoke")]
    public async Task<IActionResult> RevokeToken(int id)
    {
        var token = await dbContext.UserTokens.SingleAsync(t => t.UserId == User.GetUserId() && t.Id == id);

        token.Status = TokenStatus.Revoked;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpDelete("tokens/{id:int}")]
    public async Task<IActionResult> DeleteToken(int id)
    {
        await dbContext.UserTokens
            .Where(t => t.UserId == User.GetUserId() && t.Id == id)
            .ExecuteDeleteAsync();
        
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}