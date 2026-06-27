using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace MycroCloud.WebApp.Gateway.Telemetry;

public static class TelemetryExtensions
{
    // Adds OpenTelemetry metrics (ASP.NET Core RED metrics, outbound HTTP, .NET runtime,
    // and the gateway's custom function-execution metrics). The OTLP exporter is only
    // wired when OTEL_EXPORTER_OTLP_ENDPOINT is set (prod via Alloy); local dev without
    // it collects metrics in-process but exports nothing.
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? "gateway";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(GatewayMetrics.MeterName);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter();
                }
            });

        return services;
    }
}
