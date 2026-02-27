using Npgsql;

namespace Api.Services;

public class BuildQueuePublisher(NpgsqlDataSource dataSource, ILogger<BuildQueuePublisher> logger)
{
    public async Task PublishAsync(Guid buildId, string payloadJson)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
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
