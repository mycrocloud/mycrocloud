using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;

namespace Api.Middlewares;

public static class GlobalExceptionHandler
{
    public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var env = context.RequestServices.GetRequiredService<IHostEnvironment>();

                logger.LogError(exception,
                    "Unhandled exception for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                // Slack webhook
                if (context.Request.IsSlackCommandRequest())
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        response_type = "ephemeral",
                        text = "⚠️ An error occurred while processing your command. Please try again later."
                    });
                    return;
                }

                // General API - use ProblemDetails
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Instance = context.Request.Path
                };

                if (env.IsDevelopment())
                {
                    problemDetails.Detail = exception?.Message;
                    problemDetails.Extensions["stackTrace"] = exception?.StackTrace;
                }

                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });
    }
}