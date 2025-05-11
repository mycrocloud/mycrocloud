using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Models.Files;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;
using File = WebApp.Domain.Entities.File;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class FilesController(AppDbContext appDbContext, IRouteRepository routeRepository) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(int appId, int? folderId)
    {
        var parentFolder = folderId != null ?
            await appDbContext.Folders.Where(f => f.AppId == appId && f.Id == folderId).SingleAsync()
            : await appDbContext.Folders.Where(f => f.AppId == appId && f.ParentId == null).SingleAsync();

        const string sql =
"""
SELECT 'Folder' AS "Type", f."Id", f."Name", f."CreatedAt", NULL AS "Size", f."ParentId" 
FROM "Folders" AS f
WHERE f."ParentId" = @FolderId
UNION
SELECT 'File' AS "Type", f0."Id", f0."Name", f0."CreatedAt", LENGTH(f0."Content") AS "Size", f0."FolderId"
FROM "Files" AS f0
WHERE f0."FolderId" = @FolderId
""";
        var items = await appDbContext.Database.GetDbConnection()
        .QueryAsync<FolderContent>(sql, new
        {
            FolderId = parentFolder.Id
        });
        var pathItems = await GetFolderPathItems(appDbContext, parentFolder);
        Response.Headers.Append("Path-Items", JsonSerializer.Serialize(pathItems, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
        return Ok(items);
    }

    private static async Task<IEnumerable<FolderPathItem>> GetFolderPathItems(AppDbContext appDbContext, Folder parentFolder)
    {
        const string sql =
        """
WITH RECURSIVE FolderPath AS (
  SELECT "Id", "ParentId", 1 AS depth
  FROM public."Folders"
  WHERE "Id" = @Id
  
  UNION ALL
  
  SELECT f."Id", f."ParentId", fp."depth" + 1
  FROM public."Folders" f
  INNER JOIN FolderPath fp ON f."Id" = fp."ParentId"
)
SELECT
     ROW_NUMBER() OVER (ORDER BY depth DESC) - 1 AS Depth,
     fp."Id",
     f."Name"
FROM FolderPath fp
INNER JOIN public."Folders" f ON f."Id" = fp."Id"
ORDER BY depth;
""";

        var pathItems = await appDbContext.Database.GetDbConnection()
        .QueryAsync<FolderPathItem>(sql, new
        { 
            parentFolder.Id
        });
        return pathItems;
    }

    [HttpPost("folders")]
    public async Task<IActionResult> CreateFolder(int appId, int? folderId, CreateRenameFolderModel createFolderModel)
    {
        var parentFolder = await appDbContext.Folders
                .Include(f => f.App)
                .Where(f => f.AppId == appId && ((folderId != null && f.Id == folderId) || (folderId == null && f.ParentId == null)))
                .SingleAsync();
        var folder = new Folder
        {
            App = parentFolder.App,
            Name = createFolderModel.Name,
            Parent = parentFolder,
        };
        await appDbContext.Folders.AddAsync(folder);

        await appDbContext.SaveChangesAsync();
        return Created("", new { folder.Id, folder.Name, folder.CreatedAt });
    }

    [HttpPatch("folders")]
    public async Task<IActionResult> RenameFolder(int appId, int folderId, CreateRenameFolderModel renameFolderModel)
    {
        var folder = await appDbContext.Folders
                        .Where(f => f.AppId == appId && f.Id == folderId)
                        .SingleAsync();
        folder.Name = renameFolderModel.Name;

        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch]
    public async Task<IActionResult> Rename(int appId, int fileId, FileRenameModel fileRenameModel)
    {
        var file = await appDbContext.Files
                        .Where(f => f.Folder.AppId == appId && f.Id == fileId)
                        .SingleAsync();
        file.Name = fileRenameModel.Name;
        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("generate-route")]
    public async Task<IActionResult> GenerateRoute(int appId, int? fileId, int? folderId)
    {
        if (folderId == null && fileId == null)
        {
            return BadRequest();
        }
        var trans = await appDbContext.Database.BeginTransactionAsync();
        try
        {
            if (folderId != null)
            {
                var folder = await appDbContext.Folders
                    .Where(f => f.AppId == appId && f.Id == folderId)
                    .SingleAsync();
                await GenerateRouteForFolder(appDbContext, folder);
            }
            else
            {
                var file = await appDbContext.Files
                    .Include(f => f.Folder)
                    .Where(f => f.Folder.AppId == appId && f.Id == fileId)
                    .SingleAsync();
                var folderPathItems = await GetFolderPathItems(appDbContext, file.Folder);
                await GenerateRouteForFile(folderPathItems, file);
            }
            await trans.CommitAsync();
            return Created();
        }
        catch (Exception)
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    private async Task GenerateRouteForFolder(AppDbContext dbContext, Folder folder)
    {
        var folderPathItems = (await GetFolderPathItems(dbContext, folder)).ToList();

        foreach (var subFolder in await dbContext.Folders
            .Where(f => f.ParentId == folder.Id)
            .ToListAsync())
        {
            await GenerateRouteForFolder(dbContext, subFolder);
        }

        var files = await dbContext.Files
                .Include(f => f.Folder)
                .Where(f => f.FolderId == folder.Id)
                .ToListAsync();

        foreach (var file in files)
        {
            await GenerateRouteForFile(folderPathItems, file);
        }
    }
    
    private async Task GenerateRouteForFile(IEnumerable<FolderPathItem> folderPathItems, File file)
    {
        var items = folderPathItems.Where(f => f.Depth > 0)
                        .Select(f => f.Name.Replace(" ", "-"))
                        .Append(file.Name.Replace(" ", "-"))
                        .ToList();
        var name = string.Join('_', items);
        var path = "/" + string.Join('/', items);
        await routeRepository.Add(file.Folder.AppId, new Domain.Entities.Route
        {
            Name = name,
            Method = "GET",
            Path = path,
            ResponseType = ResponseType.StaticFile,
            ResponseStatusCode = 200,
            File = file,
            Enabled = true
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int appId, int? folderId, int? fileId)
    {
        if (folderId == null && fileId == null)
        {
            return BadRequest();
        }

        if (folderId != null)
        {
            var folder = await appDbContext.Folders
                .Where(f => f.AppId == appId && f.Id == folderId)
                .SingleAsync();
            if (folder.ParentId == null)
            {
                return BadRequest("Cannot delete root folder.");
            }
            await DeleteFolder(folderId.Value);
            await appDbContext.SaveChangesAsync();
        }
        else
        {
            var file = await appDbContext.Files
                .Where(f => f.Folder.AppId == appId && f.Id == fileId)
                .SingleAsync();
            appDbContext.Files.Remove(file);
            await appDbContext.SaveChangesAsync();
        }
        return NoContent();
    }

    private async Task DeleteFolder(int folderId)
    {
        var subFolders = await appDbContext.Folders
                .Where(f => f.ParentId == folderId)
                .ToListAsync();
        foreach (var subFolder in subFolders)
        {
            await DeleteFolder(subFolder.Id);
        }
        var files = await appDbContext.Files
                .Where(f => f.FolderId == folderId)
                .ToListAsync();
        appDbContext.Files.RemoveRange(files);
        var folder = await appDbContext.Folders
                .Where(f => f.Id == folderId)
                .SingleAsync();
        appDbContext.Folders.Remove(folder);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int appId, int? folderId, IFormFile file)
    {
        var parentFolder = await appDbContext.Folders
                .Where(f => f.AppId == appId && ((folderId != null && f.Id == folderId) || (folderId == null && f.ParentId == null)))
                .SingleAsync();

        var fileEntity = new File
        {
            Folder = parentFolder,
            Name = file.FileName,
        };
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            // Upload the file if less than 2 MB
            if (memoryStream.Length < 2 * 1024* 1024)
            {
                fileEntity.Content = memoryStream.ToArray();
            }
        }
        await appDbContext.Files.AddAsync(fileEntity);
        await appDbContext.SaveChangesAsync();
        const string getFileSizeSql = 
        """
        SELECT LENGTH(f."Content") FROM "Files" AS f WHERE f."Id" = @FileId
        """;
        var size = await appDbContext.Database.GetDbConnection()
            .ExecuteScalarAsync<int>(getFileSizeSql, new { FileId = fileEntity.Id });
        return Created("", new { fileEntity.Id, fileEntity.Name, fileEntity.CreatedAt, size });
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(int appId, int? folderId, int? fileId)
    {
        if (folderId == null && fileId == null)
        {
            return BadRequest();
        }

        if (fileId is not null)
        {
            var file = await appDbContext.Files
                        .SingleAsync(f => f.Folder.AppId == appId && f.Id == fileId);

            return File(file.Content, "application/octet-stream", file.Name);
        }

        var folder = await appDbContext.Folders
                        .SingleAsync(f => f.AppId == appId && f.Id == folderId);
        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            await AddFolderToZip(archive, folder);
        }
        zipStream.Seek(0, SeekOrigin.Begin);
        return File(zipStream, "application/zip", folder.Name + ".zip");
    }

    private async Task AddFolderToZip(ZipArchive archive, Folder folder)
    {
        var folderEntry = archive.CreateEntry(folder.Name + "/");
        foreach (var subFolder in await appDbContext.Folders
            .Where(f => f.ParentId == folder.Id)
            .ToListAsync())
        {
            await AddFolderToZip(archive, subFolder);
        }
        foreach (var file in await appDbContext.Files
            .Where(f => f.FolderId == folder.Id)
            .ToListAsync())
        {
            var fileEntry = archive.CreateEntry(folderEntry.FullName + file.Name);
            await using var entryStream = fileEntry.Open();
            await entryStream.WriteAsync(file.Content);
        }
    }
}

public class CreateRenameFolderModel
{
    [Required]
    public string Name { get; set; }
}

public class FileRenameModel
{
    [Required]
    public string Name { get; set; }
}