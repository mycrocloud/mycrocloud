# AGENTS.md — src/services/api

## Solution Structure

```
Api.sln
├── Api              — ASP.NET Core Web API (control plane)
├── Api.Domain       — Entities, enums, repository interfaces, domain services
├── Api.Infrastructure — EF Core DbContext, repositories, storage providers
├── Api.Migrations   — EF Core migrations (separate project)
└── Api.MigrationTest — Integration tests (requires live PostgreSQL)
```

## Architecture

This is the **Control Plane** of MycroCloud. Users manage apps, routes, builds, deployments, and integrations through this API. The **Data Plane** (WebApp.Gateway) is a separate service that serves deployed user apps.

### Layer Dependencies

```
Api → Api.Domain, Api.Infrastructure
Api.Migrations → Api.Infrastructure
Api.MigrationTest → Api.Migrations
Api.Domain → (no project refs — pure domain logic)
Api.Infrastructure → Api.Domain
```

## Key Patterns

### Authentication (Multi-Scheme)

Three auth schemes selected by `MultiAuthSchemes` policy based on request context:
- **Auth0 JWT** — UI users (default)
- **API Token** — M2M via `X-Api-Key` header (hashed with PBKDF2)
- **Slack** — Slack signing secret verification

### Authorization

- `AppOwnerActionFilter` enforces app ownership on most endpoints
- `[DisableAppOwnerActionFilter]` opts out specific actions
- `GitHubWebhookValidationFilter` verifies HMAC-SHA256 signatures

### Database (PostgreSQL + EF Core 8)

- **JSONB columns**: `App.Settings`, `App.CorsSettings`, `AppBuild.Metadata`, `Route.ResponseHeaders`, `AccessLog.FunctionLogs`
- **TPH inheritance**: `Deployment` base → `SpaDeployment` / `ApiDeployment` (discriminator: `"DeploymentType"`)
- **Auto-stamping**: `SaveChangesAsync()` sets `CreatedAt`, `UpdatedAt`, `Version` on all `BaseEntity` instances
- **Optimistic concurrency**: `Version` (Guid) as concurrency token, regenerated on every update
- **Cascade deletes**: Routes, RouteFolders, Variables cascade when App deleted

### Error Handling

- `GlobalExceptionHandler` middleware returns ProblemDetails (RFC 7807)
- Slack requests get JSON ephemeral messages instead of ProblemDetails

### Storage

`IStorageProvider` abstraction with two implementations:
- `S3StorageProvider` — Cloudflare R2 (production)
- `DiskStorageProvider` — local filesystem (development)

Configured via `Storage:Type` in appsettings.

### Build Pipeline

`BuildQueuePublisher` → RabbitMQ `job_queue` → Go worker (separate service) → builder container → artifact upload → `AppBuildStatusConsumer` (hosted service) consumes completion events.

## Build & Run

```bash
dotnet build src/services/api/Api.sln
dotnet run --project src/services/api/Api
dotnet test src/services/api/Api.MigrationTest  # requires live PostgreSQL
```

## Migrations

Migrations live in `Api.Migrations`. The `dotnet ef` tool may fail with `System.Runtime` version mismatch on net8.0 — create migration files manually if needed.

```bash
dotnet ef migrations add <Name> --project src/services/api/Api.Migrations
```

## Configuration

Key settings in `appsettings.json`:
- `Authentication:Schemes:Auth0JwtBearer` — Auth0 JWT config
- `ConnectionStrings:DefaultConnection` — PostgreSQL
- `ConnectionStrings:RabbitMq` — RabbitMQ
- `Storage:Type` / `Storage:S3` — artifact storage
- `ExternalIntegrations:Slack` / `ExternalIntegrations:GitHub` — integrations
- `Cors:AllowedOrigins` — CORS origins
- `WebOrigin` — frontend URL

Production config templates live in `deploy/pkg/api/`.
