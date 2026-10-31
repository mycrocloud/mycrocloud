# Durable app specifications

`app_specifications` is a normal PostgreSQL table keyed by app slug. `app_id` is
unique and references `Apps` with cascade deletion. Its JSONB value, publication
version and timestamp have no expiration. Gateway reads it directly; losing
`cache_entries` does not remove specifications.

## Application logic

All mapping, filtering, defaults and publication logic lives in C#:

- `AppSpecificationBuilder` builds runtime configuration from active deployment
  IDs, enabled authentication schemes, runtime/all variables and app settings.
- `AppSpecificationStore` loads persisted source data without tracking, invokes
  the builder, serializes JSON and upserts the value. PostgreSQL handles only
  storage, uniqueness, FK deletion and transactional row locking.
- `AppDbContext.SaveChangesAsync` identifies affected apps, locks app rows in ID
  order and publishes after saving business changes, before committing. A failed
  publication rolls back the business write. Caller transactions are preserved.
  Synchronous saves use the same path. App deletion wraps its multiple saves and
  bulk deletes in one transaction.
- Explicit publisher calls use the same store and retain existing route-cache
  pre-warming. Authentication changes publish instead of invalidating runtime data.

There are no publishing functions or triggers in the database. Build completion
only changes the published deployment when the active deployment pointer changes.
Direct SQL/bulk writes bypass change tracking; callers must explicitly publish
through the C# store in the same transaction.

## Upgrade and backfill existing data

This branch has not been applied to production by this change. The initial schema
migration now only creates the table. A follow-up migration drops the function
from the earlier branch revision if a test installation already applied it;
existing durable values remain intact. Do not deploy the earlier binaries after
that cleanup, since they still call the removed function.

Use a coordinated cutover: old API versions only update cache and cannot maintain
the durable store.

1. Back up the database, pause API configuration writes and deployment/background
   consumers, and drain in-flight writes.
2. Apply migrations through `20261031000100_RemoveAppSpecificationFunction`.
3. Run the **new** API binary in backfill mode with the same connection configuration
   as the API. It runs C# backfill and exits without starting HTTP or consumers:

   ```sh
   dotnet services/api/Api/bin/Release/net10.0/Api.dll --backfill-app-specifications
   ```

   For local development, from the configured API working directory:

   ```sh
   dotnet run --project services/api/Api -- --backfill-app-specifications
   ```

   Configuration follows the API's normal environment variables/config files.
   Backfill reconstructs every app from source tables, never from cache. It reads
   IDs in batches of 100 and commits each app independently, using the same row
   lock, builder and writer as normal publication. It is repeatable: if it fails,
   fix the cause and rerun before switching gateway traffic. Existing deployments
   and artifact storage are untouched. No app JSON or runtime secrets are logged.
4. Verify coverage and deployment pointers:

   ```sql
   SELECT a."Id", a."Slug"
   FROM "Apps" a LEFT JOIN app_specifications s ON s.app_id = a."Id"
   WHERE s.app_id IS NULL OR s.slug <> a."Slug"
      OR s.specification->>'SpaDeploymentId' IS DISTINCT FROM a."ActiveSpaDeploymentId"::text
      OR s.specification->>'ApiDeploymentId' IS DISTINCT FROM a."ActiveApiDeploymentId"::text;
   ```

   Expected: no rows. Do not enable the new gateway until backfill and verification
   succeed; applying the schema migration alone leaves existing apps unpublished.
5. Deploy the new API and gateway, then resume writes/consumers. Check active and
   disabled apps and perform a configuration update through the API.
6. Retain old `app:*` cache entries during the rollback window. After acceptance,
   they can be removed by key prefix; leave other caches alone.

For later repairs, rerun the same C# backfill command. It reloads data under the
app lock, so it does not publish stale snapshots over concurrent updates or
recreate deleted apps. Source configuration changes made outside the application
still require an explicit publication.

## Rollback

Pause writers again. Before reverting to the pre-change gateway, republish every
app into `app:{slug}` cache using the pre-change publisher and remove stale keys
for deleted/renamed apps. Retained cache entries may be stale, so reverting binaries
alone is insufficient. Keep the durable table until rollback is validated. Only
run its down migration after all new binaries have stopped.

## Validation

Use a .NET 10 SDK compatible with `global.json`:

```sh
dotnet build services/api/Api.sln
dotnet build services/webapp/gateway/Gateway.csproj
TEST_POSTGRES_CONNECTION_STRING='...' dotnet test services/api/Api.MigrationTest --filter FullyQualifiedName~AppSpecificationTests
```

The contract test exercises C# mapping and gateway deserialization without a DB.
PostgreSQL tests create random isolated schemas and cover C# backfill without
cache, logged storage, no database function, authentication, rename, owned CORS,
cache loss, deletion, active deployment, transaction rollback and concurrent writes.
Without a test connection, integration cases are explicitly skipped. Existing
migration tests use their own database configuration; use the filter above for
isolated integration tests.
