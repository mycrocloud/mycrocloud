using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using WebApp.FunctionShared;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway;

public class FunctionExecutionHub(
    [FromKeyedServices("AppDbContext")] AppDbContext appDbContext,
    RequestResponseWaiter requestResponseWaiter,
    ILogger<FunctionExecutionHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> GroupConnections = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("OnConnectedAsync");
        var appId = int.Parse(Context.GetHttpContext()!.Request.Headers["app_id"].ToString());
        logger.LogInformation("appId: {appId}", appId);

        var token = Context.GetHttpContext()!.Request.Headers["token"].ToString();

        var app = appDbContext.Apps.SingleOrDefault(a =>
            a.Id == appId && a.RegistrationTokens.Any(t => t.Token == token
                                                           && t.CreatedAt.AddHours(1) > DateTime.UtcNow
            ));

        if (app == null)
        {
            logger.LogWarning("Unauthorized");
            Context.Abort();
        }

        logger.LogInformation("Authorized");

        // Add the connection to the group
        GroupConnections.AddOrUpdate(
            appId.ToString(),
            _ => [Context.ConnectionId],
            (_, connections) =>
            {
                connections.Add(Context.ConnectionId);
                return connections;
            });

        await Groups.AddToGroupAsync(appId.ToString(), Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public void ReceiveFunctionExecutionResult(string requestId, Result result)
    {
        logger.LogInformation("ReceiveFunctionExecutionResult: {requestId}", requestId);
        requestResponseWaiter.Set(requestId, result);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("OnDisconnectedAsync");

        var appId = int.Parse(Context.GetHttpContext()!.Request.Headers["app_id"].ToString());

        // Remove connection from the group
        if (GroupConnections.TryGetValue(appId.ToString(), out var connections))
        {
            connections.Remove(Context.ConnectionId);
            if (connections.Count == 0)
            {
                GroupConnections.TryRemove(appId.ToString(), out _);
            }
        }

        await Groups.RemoveFromGroupAsync(appId.ToString(), Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // Helper to get a single connection ID from a group
    public static string? GetSingleConnection(string groupName)
    {
        if (GroupConnections.TryGetValue(groupName, out var connections) && connections.Any())
        {
            return connections.First(); // Return the first connection (custom logic can be applied)
        }

        return null;
    }
}