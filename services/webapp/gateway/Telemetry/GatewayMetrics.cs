using System.Diagnostics.Metrics;

namespace MycroCloud.WebApp.Gateway.Telemetry;

// Custom metrics for the data plane. The container-per-request function model is the
// main performance bottleneck, so we measure both the total execution time and the
// per-stage container lifecycle timings (create/start/wait).
public static class GatewayMetrics
{
    public const string MeterName = "MycroCloud.WebApp.Gateway";

    private static readonly Meter Meter = new(MeterName);

    // Total time spent executing a function (queue wait + container lifecycle), in seconds.
    // Tagged with `outcome` (success | timeout | error).
    public static readonly Histogram<double> FunctionExecutionDuration =
        Meter.CreateHistogram<double>(
            "mycrocloud.function.execution.duration",
            unit: "s",
            description: "Function execution duration in seconds.");

    // Per-stage container lifecycle timings, in seconds. Tagged with `stage`
    // (create | start | wait) to pinpoint where slow executions stall.
    public static readonly Histogram<double> FunctionStageDuration =
        Meter.CreateHistogram<double>(
            "mycrocloud.function.stage.duration",
            unit: "s",
            description: "Function container lifecycle stage duration in seconds.");
}
