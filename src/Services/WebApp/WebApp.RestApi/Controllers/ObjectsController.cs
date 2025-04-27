using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure;
using WebApp.RestApi.Filters;

namespace WebApp.RestApi.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class ObjectsController(AppDbContext appDbContext) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> ListObjects(int appId, string? prefix = null)
    {
        var app = await appDbContext.Apps
                .Include(app => app.Objects)
                .SingleAsync(app => app.Id == appId)
            ;

        var objects = app.Objects
                .Where(obj => prefix is null || obj.Key.StartsWith(prefix))
                .Select(obj => new
                {
                    obj.Key,
                    obj.CreatedAt,
                    obj.UpdatedAt
                })
                .ToList()
            ;

        return Ok(objects.Select(obj => new
        {
            obj.Key,
            obj.CreatedAt,
            obj.UpdatedAt
        }));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetObject(int appId, string key)
    {
        var app = await appDbContext.Apps
                .Include(app => app.Objects)
                .SingleAsync(app => app.Id == appId)
            ;

        var obj = app.Objects.SingleOrDefault(obj => obj.Key == key);

        if (obj is null)
        {
            return NotFound();
        }

        return File(obj.Content, "application/octet-stream");
    }

    [HttpPut("{*key}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PutObject(int appId, string key, [FromForm]IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var content = memoryStream.ToArray();
        
        var app = await appDbContext.Apps
                .Include(app => app.Objects)
                .SingleAsync(app => app.Id == appId)
            ;

        var obj = app.Objects.SingleOrDefault(obj => obj.Key == key);

        if (obj is null)
        {
            obj = new Domain.Entities.Object
            {
                Key = key,
                Content = content
            };
            app.Objects.Add(obj);
        }
        else
        {
            obj.Content = content;
        }

        await appDbContext.SaveChangesAsync();

        return Ok();
    }
}