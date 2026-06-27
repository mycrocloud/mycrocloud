using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Api.Extensions;

public static class TelemetryExtensions
{
    // Adds OpenTelemetry metrics (ASP.NET Core RED metrics, outbound HTTP, .NET runtime).
    // The OTLP exporter is only wired when OTEL_EXPORTER_OTLP_ENDPOINT is set (prod via
    // Alloy); local dev without it collects metrics in-process but exports nothing, so
    // there are no noisy connection errors.
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "api";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
