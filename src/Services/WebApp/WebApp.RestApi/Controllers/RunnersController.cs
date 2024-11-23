using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;
using WebApp.RestApi.Filters;

namespace WebApp.RestApi.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class RunnerController(AppDbContext appDbContext) : BaseController
{
    [HttpGet("registration-tokens")]
    public async Task<IActionResult> GetRegistrationTokens(int appId)
    {
        var app = await appDbContext.Apps
            .Include(x => x.RegistrationTokens)
            .SingleAsync(a => a.Id == appId);

        return Ok(app.RegistrationTokens.Select(t => new
        {
            t.Id,
            t.Token,
            t.CreatedAt
        }));
    }

    [HttpPost("registration-tokens")]
    public async Task<IActionResult> GenerateRegistrationToken(int appId)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == appId);
        var token = new AppRegistrationToken
        {
            App = app,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };
        await appDbContext.AppRegistrationTokens.AddAsync(token);
        await appDbContext.SaveChangesAsync();

        return Created(token.Id.ToString(), new { token.Id, token.Token, token.CreatedAt });;
    }
}