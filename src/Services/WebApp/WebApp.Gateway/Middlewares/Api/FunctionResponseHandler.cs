using WebApp.Domain.Entities;
using WebApp.Domain.Models;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Handler for function-based responses.
/// Executes serverless functions and returns their output.
/// </summary>
public class FunctionResponseHandler(
    IAppSpecificationService appCacheService,
    FunctionExecutorFactory executorFactory,
    ILogger<FunctionResponseHandler> logger) : IResponseHandler
{
    public ResponseType SupportedType => ResponseType.Function;

    public async Task HandleAsync(HttpContext context)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var route = (CachedRoute)context.Items["_CachedRoute"]!;

        context.Items["_FunctionRuntime"] = route.FunctionRuntime;

        var executor = executorFactory.GetExecutor(route.FunctionRuntime);
        if (executor is null)
        {
            logger.LogError("Function runtime {Runtime} not supported for route {RouteId}", 
                route.FunctionRuntime, route.Id);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Function runtime not supported.");
            return;
        }

        // Load function code from Deployment Manifest (indexed blobs)
        var functionCode = await appCacheService.GetApiDeploymentFileContentAsync(app.ApiDeploymentId, $"routes/{route.Id}/content");
        if (string.IsNullOrEmpty(functionCode))
        {
            logger.LogError("Function code not found for route {RouteId} in deployment {DeploymentId}", 
                route.Id, app.ApiDeploymentId);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Function code not found.");
            return;
        }

        logger.LogDebug("Executing function for route {RouteId} with runtime {Runtime}", 
            route.Id, route.FunctionRuntime);

        var result = await executor.ExecuteAsync(context, app, functionCode, null);

        await context.Response.WriteFromFunctionResult(result);

        context.Items["_FunctionExecutionResult"] = result;
        
        logger.LogDebug("Function execution completed for route {RouteId}: StatusCode={StatusCode}",
            route.Id, result.StatusCode);
    }
}
