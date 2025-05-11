using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApp.Infrastructure;
using WebApp.Api.Extensions;

namespace WebApp.Api.Filters;

public class AppOwnerActionFilter(AppDbContext appDbContext,
    ILogger<AppOwnerActionFilter> logger, string appIdArgumentName = "appId")
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await DoWork(context))
        {
            // short-circuit
            return;
        }
        
        await next();
    }

    private async Task<bool> DoWork(ActionExecutingContext context)
    {
        logger.LogDebug("Executing AppOwnerActionFilter");
        if (context.HttpContext.User.Identity is null || !context.HttpContext.User.Identity.IsAuthenticated)
        {
            logger.LogDebug("User is not authenticated");
            return true;
        }
        var userId = context.HttpContext.User.GetUserId();
        logger.LogDebug("UserId: {UserId}", userId);
        
        if (!TryGetAppId(context, out var appId))
        {
            logger.LogDebug("AppId argument is missing");
            return true;
        }
        
        logger.LogDebug("AppId: {AppId}", appId);

        var app = await appDbContext.Apps.FindAsync(appId);
        var isAppOwner = app?.UserId == userId;
        logger.LogDebug("IsAppOwner: {IsAppOwner}", isAppOwner);
        if (!isAppOwner)
        {
            logger.LogDebug("User {UserId} is not the owner of the app {AppId}", userId, appId);
            context.Result = new ForbidResult();
            return false;
        }
        
        logger.LogDebug("User {UserId} is the owner of the app {AppId}", userId, appId);
        context.HttpContext.Items["App"] = app!;
        
        return true;
    }

    private bool TryGetAppId(ActionExecutingContext context, out int? appId)
    {
        appId = null;
        
        // Get from RouteData, ActionArguments.
        if (context.RouteData.Values.TryGetValue(appIdArgumentName, out var routeValue))
        {
            appId = int.Parse(routeValue!.ToString() ?? throw new InvalidOperationException());
            return true;
        }
        
        if (context.ActionArguments.TryGetValue(appIdArgumentName, out var actionValue))
        {
            appId = int.Parse(actionValue!.ToString() ?? throw new InvalidOperationException());
            return true;
        }
        
        return false;
    }

}