using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebApp.FunctionShared;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway;

public class FunctionExecutionHub(
    [FromKeyedServices("AppDbContext")] AppDbContext appDbContext,
    RequestResponseWaiter requestResponseWaiter,
    ILogger<FunctionExecutionHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> GroupConnections = new();

    private readonly Dictionary<string, string> _connectionIdToUserId = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("OnConnectedAsync");

        try
        {
            var request = Context.GetHttpContext()!.Request;
            var token = request.Headers["token"].ToString();

            var registrationToken = await appDbContext.RunnerRegistrationTokens
                .Include(t => t.App)
                .SingleAsync(t => t.Token == token);

            var userId = registrationToken.UserId ?? registrationToken.App.UserId;

            GroupConnections.AddOrUpdate(
                userId,
                _ => [Context.ConnectionId],
                (_, connections) =>
                {
                    connections.Add(Context.ConnectionId);
                    return connections;
                });

            _connectionIdToUserId[Context.ConnectionId] = userId;

            await base.OnConnectedAsync();
        }
        catch (Exception e)
        {
            Context.Abort();
            logger.LogError(e, "Error in OnConnectedAsync");
            throw;
        }
    }

    public void ReceiveFunctionExecutionResult(string requestId, Result result)
    {
        logger.LogInformation("ReceiveFunctionExecutionResult: {requestId}", requestId);
        requestResponseWaiter.Set(requestId, result);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("OnDisconnectedAsync");

        if (_connectionIdToUserId.TryGetValue(Context.ConnectionId, out var userId))
        {
            if (GroupConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                {
                    GroupConnections.TryRemove(userId, out _);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string? GetSingleConnection(string userId)
    {
        if (GroupConnections.TryGetValue(userId, out var connections) && connections.Any())
        {
            return connections.First();
        }

        return null;
    }
}