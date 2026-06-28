namespace MycroCloud.WebApp.Gateway.Configuration;

// Bound from the "DockerFunctionExecution" config section. Holds the host-level
// concurrency cap and the per-plan resource limits applied to each function container.
public class FunctionExecutionOptions
{
    // Max number of function containers allowed to run concurrently on this host.
    // This protects the host (CPU/RAM) and is intentionally NOT per-plan.
    public int MaxConcurrency { get; set; }

    // Plan applied to every account until per-account plans are persisted.
    public string DefaultPlan { get; set; } = "";

    // plan name -> resource limits
    public Dictionary<string, FunctionPlan> Plans { get; set; } = [];
}

// Per-invocation resource limits for a single function container.
public class FunctionPlan
{
    public int MemoryMb { get; set; }
    public double Cpu { get; set; }
    public int TimeoutSeconds { get; set; }
}

// The plan resolved for a given app, including its name (for tagging/diagnostics).
public record ResolvedFunctionPlan(string Name, FunctionPlan Limits);
