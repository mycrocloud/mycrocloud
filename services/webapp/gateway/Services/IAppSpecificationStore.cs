using System.Text.Json;
using MycroCloud.WebApp.Gateway.Models;
using Npgsql;

namespace MycroCloud.WebApp.Gateway.Services;

public interface IAppSpecificationStore
{
    Task<AppSpecification?> GetBySlugAsync(string slug);
}

/// <summary>Read-only access to published, durable runtime configuration.</summary>
public sealed class PostgresAppSpecificationStore(NpgsqlDataSource dataSource) : IAppSpecificationStore
{
    public async Task<AppSpecification?> GetBySlugAsync(string slug)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT specification::text FROM app_specifications WHERE slug = @slug");
        command.Parameters.AddWithValue("slug", slug);
        var json = await command.ExecuteScalarAsync() as string;
        return json == null ? null : JsonSerializer.Deserialize<AppSpecification>(json)
            ?? throw new JsonException("Published app specification must be an object.");
    }
}
