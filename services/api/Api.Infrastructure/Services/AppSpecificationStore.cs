using System.Text.Json;
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Services;

/// <summary>Publishes C#-built specifications using the caller's DbContext and transaction.</summary>
public sealed class AppSpecificationStore(AppDbContext dbContext)
{
    public async Task<bool> PublishAsync(int appId, CancellationToken cancellationToken = default)
    {
        await using var transaction = dbContext.Database.CurrentTransaction == null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Apps\" WHERE \"Id\" = {appId} FOR UPDATE", cancellationToken);

        // Read persisted values, including writes just saved in the current transaction.
        // Tracking would reuse stale navigation collections from the caller's unit of work.
        var app = await dbContext.Apps.AsNoTracking()
            .Include(a => a.AuthenticationSchemes.Where(s => s.Enabled))
            .Include(a => a.Variables.Where(v => v.Target == VariableTarget.Runtime || v.Target == VariableTarget.All))
            .SingleOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (app == null)
        {
            // Deletion is handled by the FK cascade; never recreate a deleted app.
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var json = JsonSerializer.Serialize(AppSpecificationBuilder.Build(app));
        var version = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app_specifications (slug, app_id, specification, version, updated_at)
            VALUES ({app.Slug}, {app.Id}, {json}::jsonb, {version}, {updatedAt})
            ON CONFLICT (app_id) DO UPDATE SET
                slug = EXCLUDED.slug, specification = EXCLUDED.specification,
                version = EXCLUDED.version, updated_at = EXCLUDED.updated_at
            """, cancellationToken);
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Repeatable, bounded-memory backfill from source data, never from cache.</summary>
    public async Task<int> BackfillAsync(CancellationToken cancellationToken = default)
    {
        var lastId = 0;
        var published = 0;
        while (true)
        {
            var ids = await dbContext.Apps.AsNoTracking().Where(a => a.Id > lastId)
                .OrderBy(a => a.Id).Select(a => a.Id).Take(100).ToListAsync(cancellationToken);
            if (ids.Count == 0) return published;
            foreach (var id in ids)
            {
                if (await PublishAsync(id, cancellationToken)) published++;
                lastId = id;
            }
        }
    }
}
