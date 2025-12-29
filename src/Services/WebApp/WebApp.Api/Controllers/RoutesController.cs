using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Models.Routes;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Infrastructure;
using Route = WebApp.Domain.Entities.Route;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class RoutesController(
    AppDbContext appDbContext
) : BaseController
{
    private App App => (HttpContext.Items["App"] as App)!;

    [HttpGet]
    public async Task<IActionResult> List(string? q, string? s)
    {
        var folders = appDbContext.RouteFolders
            .Where(f => f.App == App)
            .Select(f => new RouteFolderRouteItem
            {
                Type = RouteRouteFolderType.Folder,
                Id = f.Id,
                ParentId = f.Parent != null ? f.Parent.Id : null,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                RouteName = null,
                RouteMethod = null,
                RoutePath = null,
                RouteStatus = null,
                FolderName = f.Name
            });

        var routes = appDbContext.Routes
            .Where(r => r.App == App)
            .Select(r => new RouteFolderRouteItem
            {
                Type = RouteRouteFolderType.Route,
                Id = r.Id,
                ParentId = r.Folder != null ? r.Folder.Id : null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                RouteName = r.Name,
                RouteMethod = r.Method,
                RoutePath = r.Path,
                RouteStatus = r.Status,
                FolderName = null
            });

        var items = await folders.Union(routes).ToListAsync();

        return Ok(items.Select(RouteFolderRouteItem));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var route = await appDbContext.Routes
            .SingleAsync(r => r.App == App && r.Id == id);

        return Ok(RouteDetails(route));
    }

    private static object RouteDetails(Route route)
    {
        return new
        {
            route.Id,
            route.AppId,
            route.Name,
            route.Method,
            route.Path,
            route.RequestQuerySchema,
            route.RequestHeaderSchema,
            route.RequestBodySchema,
            ResponseType= route.ResponseType.ToString(),
            route.ResponseStatusCode,
            ResponseHeaders = (route.ResponseHeaders ?? []).Select(h => new
            {
                h.Name,
                h.Value
            }),
            route.UseDynamicResponse,
            route.Response,
            route.ResponseBodyLanguage,
            route.FunctionHandlerDependencies,
            route.RequireAuthorization,
            Status = route.Status.ToString(),
            route.Enabled,
            route.CreatedAt,
            route.UpdatedAt
        };
    }

    [HttpPost("{id:int}/Clone")]
    public async Task<IActionResult> Clone(int id)
    {
        var route = await appDbContext.Routes.AsNoTracking().SingleAsync(r => r.App == App && r.Id == id);
        route.Id = 0;
        route.Name += " - Copy";

        await appDbContext.Routes.AddAsync(route);
        await appDbContext.SaveChangesAsync();

        return Created(route.Id.ToString(), RouteDetails(route));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RouteCreateUpdateRequest createRequest)
    {
        var route = createRequest.ToCreateEntity();

        route.Folder = createRequest.FolderId != null
            ? await appDbContext.RouteFolders.SingleAsync(f =>
                f.App == App && f.Id == createRequest.FolderId.Value)
            : null;

        route.App = App;

        await appDbContext.Routes.AddAsync(route);
        await appDbContext.SaveChangesAsync();

        return Created(route.Id.ToString(), new { route.Id, route.Version });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, RouteCreateUpdateRequest updateRequest)
    {
        var route = await appDbContext.Routes
            .SingleAsync(r => r.App == App && r.Id == id);

        if (route.Status == RouteStatus.Blocked)
        {
            // Do not allow editing blocked route   
            return BadRequest();
        }

        updateRequest.ToUpdateEntity(ref route);

        await appDbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await appDbContext.Routes.SingleAsync(r => r.App == App && r.Id == id);
        if (route.Status == RouteStatus.Blocked)
        {
            // Do not allow deleting blocked route
            return BadRequest();
        }

        appDbContext.Routes.Remove(route);
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("folders")]
    public async Task<IActionResult> CreateFolder(FolderCreateRequest folderCreateRequest)
    {
        var parentFolder = folderCreateRequest.ParentId != null
            ? await appDbContext.RouteFolders.SingleAsync(f =>
                f.App == App && f.Id == folderCreateRequest.ParentId.Value)
            : null;

        var newFolder = new RouteFolder
        {
            App = App,
            Name = folderCreateRequest.Name,
            Parent = parentFolder,
            Version = Guid.NewGuid()
        };

        await appDbContext.RouteFolders.AddAsync(newFolder);

        await appDbContext.SaveChangesAsync();

        return Created(newFolder.Id.ToString(), new
        {
            newFolder.Id,
            newFolder.Name,
            newFolder.Version
        });
    }

    [HttpPost("folders/{id:int}/duplicate")]
    public async Task<IActionResult> DuplicateFolder(int id)
    {
        var folder = await appDbContext.RouteFolders
            .Include(routeFolder => routeFolder.Parent)
            .SingleAsync(f => f.App == App && f.Id == id);

        var newFolder = await RecursiveDuplicateFolder(folder, folder.Parent, true);

        await appDbContext.SaveChangesAsync();

        const string sql = """
                           WITH RECURSIVE FolderHierarchy AS (
                               SELECT "Id", "ParentId", "Name"
                               FROM "RouteFolders"
                               WHERE "Id" = @id
                           
                               UNION ALL
                           
                               SELECT rf."Id", rf."ParentId", rf."Name"
                               FROM "RouteFolders" rf
                                        JOIN FolderHierarchy fh ON fh."Id" = rf."ParentId"
                           )
                           SELECT
                                1 AS "Type",
                                "Id",
                                "ParentId",
                                NULL AS "RouteName",
                                NULL AS "RouteMethod",
                                NULL AS "RoutePath",
                                NULL AS "RouteStatus",
                                "Name" AS "FolderName"
                           FROM
                                FolderHierarchy
                                
                           UNION ALL

                           SELECT
                                2 AS "Type",
                                "Id",
                                "FolderId" AS "ParentId",
                                "Name" AS "RouteName",
                                "Method" AS "RouteMethod",
                                "Path" AS "RoutePath",
                                "Status" AS "RouteStatus",
                                NULL AS "FolderName"
                           FROM
                                "Routes"
                           WHERE
                                "FolderId" IN (SELECT "Id" FROM FolderHierarchy);
                           """;
        var createdItems = await appDbContext.Database.GetDbConnection().QueryAsync<RouteFolderRouteItem>(sql, new
        {
            id = newFolder.Id
        });

        return Created(newFolder.Id.ToString(), createdItems.Select(RouteFolderRouteItem));
    }

    private async Task<RouteFolder> RecursiveDuplicateFolder(RouteFolder sourceFolder, RouteFolder parent,
        bool isTop = false)
    {
        var newFolder = new RouteFolder
        {
            App = App,
            Name = isTop ? sourceFolder.Name + " - Copy" : sourceFolder.Name,
            Parent = parent
        };

        await appDbContext.RouteFolders.AddAsync(newFolder);

        var routes = await appDbContext.Routes
            .Where(r => r.App == App && r.Folder == sourceFolder)
            .AsNoTracking()
            .ToListAsync();

        foreach (var route in routes)
        {
            route.Id = 0; // Reset ID to create new
            route.Folder = newFolder;
        }

        await appDbContext.Routes.AddRangeAsync(routes);

        var subFolders = await appDbContext.RouteFolders
            .Where(f => f.App == App && f.Parent == sourceFolder)
            .AsNoTracking()
            .ToListAsync();

        foreach (var subFolder in subFolders)
        {
            await RecursiveDuplicateFolder(subFolder, newFolder);
        }

        //return top folder
        return newFolder;
    }

    [HttpDelete("folders/{id:int}")]
    public async Task<IActionResult> DeleteFolder(int id)
    {
        var folder = await appDbContext.RouteFolders.SingleAsync(f => f.App == App && f.Id == id);

        const string sql =
            """
            CREATE TEMP TABLE folders_to_delete AS (
               WITH RECURSIVE cte AS (
                   SELECT "Id" FROM "RouteFolders" WHERE "Id" = @id
                   UNION ALL
                   SELECT rf."Id" FROM "RouteFolders" rf JOIN cte ON rf."ParentId" = cte."Id"
               )
               SELECT "Id" FROM cte
            );

            DELETE FROM "Routes" WHERE "FolderId" IN (SELECT "Id" FROM folders_to_delete);
            DELETE FROM "RouteFolders" WHERE "Id" IN (SELECT "Id" FROM folders_to_delete);

            SELECT
                1 AS "Type",
                "Id", "ParentId",
                NULL AS "RouteName",
                NULL AS "RouteMethod",
                NULL AS "RoutePath",
                NULL AS "RouteStatus",
                "Name" AS "FolderName"
            FROM
                "RouteFolders"
            WHERE
                "Id" IN (SELECT "Id" FROM folders_to_delete)
                
            UNION ALL

            SELECT
                2 AS "Type",
                "Id",
                "FolderId" AS "ParentId",
                "Name" AS "RouteName",
                "Method" AS "RouteMethod",
                "Path" AS "RoutePath",
                "Status" AS "RouteStatus",
                NULL AS "FolderName"
            FROM
                "Routes"
            WHERE
                "FolderId" IN (SELECT "Id" FROM folders_to_delete);
            """;

        var deletedItems = await appDbContext.Database.GetDbConnection()
            .QueryAsync<RouteFolderRouteItem>(sql, new { id = folder.Id });

        return Ok(deletedItems.Select(RouteFolderRouteItem));
    }

    [HttpPatch("folders/{id:int}/rename")]
    public async Task<IActionResult> RenameFolder(int id, FolderRenameRequest folderRenameRequest)
    {
        var folder = await appDbContext.RouteFolders.SingleAsync(f => f.App == App && f.Id == id);
        folder.Name = folderRenameRequest.Name;
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    private static object RouteFolderRouteItem(RouteFolderRouteItem item)
    {
        return new
        {
            Type = item.Type.ToString(),
            item.Id,
            item.ParentId,
            Route = item.Type == RouteRouteFolderType.Route
                ? new
                {
                    Name = item.RouteName,
                    Method = item.RouteMethod,
                    Path = item.RoutePath,
                    Status = item.RouteStatus.ToString()
                }
                : null,
            Folder = item.Type == RouteRouteFolderType.Folder
                ? new
                {
                    Name = item.FolderName
                }
                : null
        };
    }
}