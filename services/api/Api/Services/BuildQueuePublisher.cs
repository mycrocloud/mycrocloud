using Npgsql;

namespace Api.Services;

public class PubSubDataSource : IDisposable
{
    public NpgsqlDataSource DataSource { get; }

    public PubSubDataSource(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PubSub")!;
        DataSource = NpgsqlDataSource.Create(connectionString);
    }

    public void Dispose() => DataSource.Dispose();
}

public class BuildQueuePublisher(PubSubDataSource pubSub, ILogger<BuildQueuePublisher> logger)
{
    public async Task PublishAsync(Guid buildId, string payloadJson)
    {
        await using var conn = await pubSub.DataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO build_queue (id, payload) VALUES (@id, @payload::jsonb);
            NOTIFY build_job_available;
            """;
        cmd.Parameters.AddWithValue("id", buildId);
        cmd.Parameters.AddWithValue("payload", payloadJson);
        await cmd.ExecuteNonQueryAsync();

        logger.LogDebug("Queued build job {BuildId}", buildId);
    }
}
