using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MockServer.Data;
using MockServer.DTOs;
using MockServer.Models;

namespace MockServer.Services;

public class ActionResultService : IActionResultService
{
    private readonly AppDbContext dbContext;

    public ActionResultService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    public async Task<CustomActionResult> GetActionResult(RequestDto request)
    {

        // SELECT Response.StatusCode
        // FROM Requests
        // INNER JOIN User
        // I
        // WHERE username = @username AND workspace = @workspace AND requestId = @requestId

        var user = await dbContext.Users
                        .Include(u => u.Workspaces.Where(w => w.Name == request.Workspace))
                        .ThenInclude(w => w.Requests.Where(req => req.Method == request.Method && req.Path == request.Path))
                        .ThenInclude(r => r.Response)
                        .Where(u => u.Username == request.Username)
                        .FirstOrDefaultAsync();


        Response response = null;
        if (user != null)
        {
            var workspace = user.Workspaces.FirstOrDefault();
            if (workspace != null)
            {
                var req = workspace.Requests.FirstOrDefault();
                if (req != null)
                {
                    response = req.Response;
                }
            }
        }
        return new CustomActionResult(response);
    }
}