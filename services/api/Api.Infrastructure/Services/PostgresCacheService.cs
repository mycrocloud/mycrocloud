using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

namespace Api.Infrastructure.Services;

public class PostgresCacheService(NpgsqlDataSource dataSource) : IDistributedCache
{
    public byte[]? Get(string key)
    {
        return GetAsync(key, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        await using var cmd = dataSource.CreateCommand(
            "SELECT value FROM cache_entries WHERE key = @key AND (expires_at IS NULL OR expires_at > now())");
        cmd.Parameters.AddWithValue("key", key);

        await using var reader = await cmd.ExecuteReaderAsync(token);
        if (await reader.ReadAsync(token))
        {
            return (byte[])reader[0];
        }

        return null;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        DateTimeOffset? expiresAt = null;
        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            expiresAt = DateTimeOffset.UtcNow + options.AbsoluteExpirationRelativeToNow.Value;
        }
        else if (options.AbsoluteExpiration.HasValue)
        {
            expiresAt = options.AbsoluteExpiration.Value;
        }
        else if (options.SlidingExpiration.HasValue)
        {
            expiresAt = DateTimeOffset.UtcNow + options.SlidingExpiration.Value;
        }

        await using var cmd = dataSource.CreateCommand(
            """
            INSERT INTO cache_entries (key, value, expires_at)
            VALUES (@key, @value, @expires_at)
            ON CONFLICT (key) DO UPDATE SET value = @value, expires_at = @expires_at
            """);
        cmd.Parameters.AddWithValue("key", key);
        cmd.Parameters.AddWithValue("value", value);
        cmd.Parameters.AddWithValue("expires_at", expiresAt.HasValue ? expiresAt.Value : DBNull.Value);

        await cmd.ExecuteNonQueryAsync(token);
    }

    public void Refresh(string key)
    {
        // No-op: only absolute expiration is used
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        // No-op: only absolute expiration is used
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        RemoveAsync(key, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await using var cmd = dataSource.CreateCommand("DELETE FROM cache_entries WHERE key = @key");
        cmd.Parameters.AddWithValue("key", key);
        await cmd.ExecuteNonQueryAsync(token);
    }
}
