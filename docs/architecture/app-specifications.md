# Durable app specifications

`app_specifications` is the published runtime configuration store. Its key is the
app slug; `app_id` is unique and references `Apps` with cascade deletion. The
JSONB value, publication version and timestamp have no expiration. Gateway reads
this table directly. Cache cleanup and loss of the UNLOGGED `cache_entries` table
do not remove app specifications.

## Writes and consistency

`AppDbContext.SaveChangesAsync` (including the synchronous overload) finds changed
apps, authentication schemes, variables and app-owned settings. It locks owning
app rows in ascending ID order, writes business data, and calls
`refresh_app_specification` before committing. A projection failure rolls back the
business write. Existing caller transactions are preserved. App deletion's
multiple saves and bulk deletes are wrapped in one repository transaction.

The PostgreSQL function is the canonical JSON projection used both for live writes
and migration backfill. It uses active deployment IDs, enabled authentication
schemes, runtime/all variables and the application's default settings. Draft
builds do not become active deployments through this mechanism. Publishing does
not copy route bodies or build-only variables into the specification.

Explicit `IAppSpecificationPublisher` calls rebuild the durable value and retain
existing route-cache pre-warming. They are not required to recover an app after a
cache miss. Authentication changes publish instead of deleting runtime data.

The store/function are managed by SQL migration, like `cache_entries`; they are
intentionally outside EF's entity model. Future specification fields/defaults
must update the SQL function in a **new migration**, both C# contracts, and the
integration tests. Direct SQL/bulk changes to app configuration must invoke the
refresh function in the same transaction; they bypass EF change tracking.

## Upgrade existing installations

This release requires a coordinated cutover; do not run old API writers alongside
the new gateway. Old API versions only update cache and cannot maintain this store.

1. Back up the database and pause API mutation endpoints and background consumers
   that write app configuration or activate deployments. Drain in-flight writes.
2. Apply migration `20261031000000_AddDurableAppSpecifications`. It creates a normal
   logged table, locks source tables during backfill and reconstructs **all** apps
   from `Apps`, `AuthenticationSchemes` and `Variables`. Expired/missing/malformed
   old cache values are never used. Existing deployments and storage are untouched.
3. Verify coverage and deployment pointers before starting the new gateway:

   ```sql
   SELECT a."Id", a."Slug"
   FROM "Apps" a LEFT JOIN app_specifications s ON s.app_id = a."Id"
   WHERE s.app_id IS NULL OR s.slug <> a."Slug"
      OR s.specification->>'SpaDeploymentId' IS DISTINCT FROM a."ActiveSpaDeploymentId"::text
      OR s.specification->>'ApiDeploymentId' IS DISTINCT FROM a."ActiveApiDeploymentId"::text;
   ```

   Expected: no rows. Inspect app specifications through authorized operational
   access only: values contain runtime secrets. Do not log/dump JSON values.
4. Deploy the new API and gateway, then resume writes/consumers. Check active,
   disabled and renamed apps through gateway and perform a configuration update.
5. Leave old `app:*` cache entries alone during the rollback window. Once the
   release is accepted they can be deleted by key prefix; route/signing-key caches
   must remain. No production cleanup is performed automatically.

Backfill is atomic with schema creation. A failed migration rolls back. A repair
can rebuild specifications without changing deployments, in a transaction:

```sql
BEGIN;
SELECT refresh_app_specification("Id") FROM "Apps" ORDER BY "Id";
COMMIT;
```

Each refresh locks its owning app and is repeatable. On large installations,
benchmark migration time on a restored database before scheduling the cutover.

## Rollback

Pause writers and consumers again. Before reverting to the old gateway, republish
**every** app into the old `app:{slug}` cache using the previous release's publisher,
and remove stale keys for deleted/renamed apps. Old cache entries retained during
upgrade may be stale; reverting binaries alone is insufficient. Keep the durable
table until the rollback is validated. The down migration drops it and its refresh
function, and must only run after new binaries have stopped.

## Validation

Run with a .NET 10 SDK compatible with `global.json`:

```sh
dotnet build services/api/Api.sln
dotnet build services/webapp/gateway/Gateway.csproj
TEST_POSTGRES_CONNECTION_STRING='...' dotnet test services/api/Api.MigrationTest --filter FullyQualifiedName~AppSpecificationTests
```

The new PostgreSQL tests create random isolated schemas and drop only those schemas
on completion. They test backfill without cache, persisted defaults and filtering,
logged storage, gateway reads, authentication updates, rename, owned CORS changes,
cache loss, delete, deployment activation, transaction visibility, concurrent child
updates, and rollback after projection failure. A database-independent contract test
checks that SQL defaults still match domain defaults.
The existing migration tests have their own connection configuration and can
migrate that configured database; choose the filter above for isolated tests.
