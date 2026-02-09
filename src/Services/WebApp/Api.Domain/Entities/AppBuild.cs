namespace Api.Domain.Entities;

public class AppBuild : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public string Status { get; set; }
    public DateTime? FinishedAt { get; set; }
}

public class AppBuildState
{
    public const string queued = "queued";
    public const string running = "running";
    public const string succeeded = "succeeded";
    public const string failed = "failed";
}