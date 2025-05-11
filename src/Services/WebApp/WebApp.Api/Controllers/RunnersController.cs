using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;
using WebApp.Api.Extensions;

namespace WebApp.Api.Controllers;

[Route("apps/[controller]")]
public class RunnerController(AppDbContext appDbContext) : BaseController
{
    [HttpGet("registration-tokens")]
    public async Task<IActionResult> GetUserRegistrationTokens()
    {
        var tokens = await appDbContext.RunnerRegistrationTokens
            .Where(t => t.UserId == User.GetUserId())
            .ToListAsync();

        return Ok(tokens.Select(t => new
        {
            t.Id,
            t.Scope,
            t.Token,
            t.CreatedAt
        }));
    }

    [HttpPost("registration-tokens")]
    public async Task<IActionResult> GenerateUserRegistrationToken()
    {
        var token = new RunnerRegistrationToken
        {
            UserId = User.GetUserId(),
            Token = Guid.NewGuid().ToString(),
            Scope = RunnerRegistrationTokenScope.User,
            CreatedAt = DateTime.UtcNow
        };
        await appDbContext.RunnerRegistrationTokens.AddAsync(token);
        await appDbContext.SaveChangesAsync();

        return Created(token.Id.ToString(), new { token.Id, token.Token, token.CreatedAt });
    }

    [HttpGet("{appId:int}/registration-tokens")]
    [TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
    public async Task<IActionResult> GetAppRegistrationTokens(int appId)
    {
        var app = await appDbContext.Apps
            .Include(a => a.RegistrationTokens)
            .SingleAsync(a => a.Id == appId);

        return Ok(app.RegistrationTokens.Select(t => new
        {
            t.Id,
            t.Scope,
            t.Token,
            t.CreatedAt
        }));
    }

    [HttpPost("{appId:int}/registration-tokens")]
    [TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
    public async Task<IActionResult> GenerateAppRegistrationToken(int appId)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
        var token = new RunnerRegistrationToken
        {
            App = app,
            Scope = RunnerRegistrationTokenScope.App,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
        await appDbContext.RunnerRegistrationTokens.AddAsync(token);
        await appDbContext.SaveChangesAsync();

        return Created(token.Id.ToString(), new { token.Id, token.Token, token.CreatedAt });
    }
}