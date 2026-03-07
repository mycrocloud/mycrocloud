# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Backend (.NET 10, C# 14)
```bash
# Build API solution (control plane)
dotnet build src/services/api/Api.sln

# Build WebApp solution (data plane + shared libs)
dotnet build src/services/webapp/WebApp.sln

# Run the API (control plane)
dotnet run --project src/services/api/Api

# Run the Gateway (data plane)
dotnet run --project src/services/webapp/gateway

# Run migration tests (requires live PostgreSQL)
dotnet test src/services/api/Api.MigrationTest

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project src/services/api/Api.Migrations

# Run db-migrator (applies pending migrations)
dotnet run --project src/services/api/Api.Migrations
```

### Frontend (React 19 + Vite + TypeScript)
```bash
npm run dev --prefix src/services/web        # Dev server
npm run build --prefix src/services/web      # Production build (tsc + vite)
npm run lint --prefix src/services/web       # ESLint
```

### Go Deployment Worker
```bash
cd src/services/webapp/spa/worker && go build ./...
```

### Docker Compose (dev)
```bash
# Frontend only
docker compose -f src/services/compose.yml up

# API + db-migrator (run from api dir so compose.override.yml is picked up)
cd src/services/api && docker compose build && docker compose up -d

# Gateway + function-invoker (run from webapp dir)
cd src/services/webapp && docker compose build && docker compose up -d

# Deployment subsystem (worker + builder)
cd src/services/webapp/spa && docker compose up -d
```

## Architecture Overview

**MycroCloud** is a platform for deploying managed web apps (API, SPA, or Fullstack). It has a **dual-plane** design:

- **Control Plane** (`Api` + `Web`): Users manage apps, routes, builds, deployments, and integrations
- **Data Plane** (`WebApp.Gateway`): Serves deployed user apps — routes requests to static content, SPA files (from S3/disk), or executes JS functions via Docker containers

### Service Topology (Production)
```
Nginx LB ─┬─ web (React SPA)
           ├─ api (ASP.NET Core control plane)
           └─ gateway (ASP.NET Core data plane, caches app specs in Redis)

Deployment subsystem (separate compose):
  worker (Go) ── listens RabbitMQ "job_queue" ── spawns builder containers
  builder (Node.js) ── git clone + npm build + zip artifacts

Monitoring (Prometheus + Alloy, config in deploy/pkg/monitoring/)
```

### Project Dependencies
```
src/services/api/
  Api              → Api.Domain, Api.Infrastructure
  Api.Migrations   → Api.Infrastructure
  Api.MigrationTest

src/services/webapp/
  WebApp.Gateway                              (self-contained data plane service)
  api/functioninvoker/WebApp.FunctionInvoker  (standalone console app for JS execution)

Api.Domain         — entities, enums, repository interfaces, domain services
Api.Infrastructure — AppDbContext (EF Core), repositories, storage providers
```

### SPA Build Pipeline
Build jobs queued via RabbitMQ → Go `worker` spawns `builder` container (Node.js) → clones repo, builds, zips output → uploads to S3/R2 → publishes completion event back via RabbitMQ → API updates deployment status.

### Function Execution
Gateway spawns a `WebApp.FunctionInvoker` Docker container per request. Uses Jint (embedded JS engine) with a 3-second timeout. Data passed via mounted files at `/tmp/function-data`.

### Storage
`IStorageProvider` abstraction with `DiskStorageProvider` and `S3StorageProvider` (Cloudflare R2 in prod). Configured via `Storage:Type` setting.

### Authentication
Multi-scheme auth: Auth0 JWT (UI users), custom API tokens (`X-Api-Key` header), Slack auth. Scheme selected by `MultiAuthSchemes` policy based on request host prefix.

### Database
PostgreSQL with EF Core 10 (Npgsql). Uses JSONB columns, owned JSON entities for settings, TPH discriminator for `Deployment` → `SpaDeployment`/`ApiDeployment`. `AppDbContext` auto-stamps `CreatedAt`/`UpdatedAt`/`Version` in `SaveChangesAsync`.

Migrations live in a separate `Api.Migrations` project. Design-time factory in `AppDbContextFactory`.

### Frontend Config Injection
`window.CONFIG` injected at runtime via Nginx (allows single Docker image across environments). Falls back to `VITE_*` env vars for local dev. See `src/services/web/src/config.ts`.

### CI/CD
GitHub Actions per-service (path-filtered pushes to `main`). Reusable workflows: `_build-image.yml`, `_deploy-image.yml`, `_build-deploy.yml`: build Docker image → push to `ghcr.io` → SSH + Ansible playbook → `docker compose up`. Secrets from AWS Secrets Manager.

Per-service workflows: `api.yml`, `gateway.yml`, `web.yml`, `function-invoker.yml`, `db-migrator.yml`, `spa-build-worker.yml`, `spa-builder.yml`. `deploy-all.yml` orchestrates a full stack redeploy.

### Deployment Config Files
Production config files (`appsettings.json`, `.env.j2` templates) live in `deploy/pkg/`, mirroring `src/services/` paths. When changing config schema (e.g., adding/renaming settings in `appsettings.json`), update the corresponding files in `deploy/pkg/` as well. When adding or removing a service's `.env` or secret file, also update `deploy/scripts/deploy.yml` secret lists and `deploy/infra/aws_secrets.tf`.

### Documentation project
The documentation project lives in a separate repository:
```
../mycrocloud-docs
```

### Coding Conventions

**Internal service-to-service communication**

Internal services (e.g. API → worker → builder) fully trust the caller. The target service must not validate input or apply default values — if required data is missing, let it fail loudly so the bug surfaces in the caller.

- No required-field checks (null guards, `[Required]` attributes, etc.)
- No fallback defaults (no `${VAR:-default}`, no property initializers with meaningful values)
- No length or format validation

Examples of what NOT to do:
```bash
# Bad: silently falls back to "dist" if caller forgot to set OUT_DIR
OUT_DIR="${OUT_DIR:-dist}"
```
```csharp
// Bad: hides a bug in the caller that forgot to set NodeVersion
public string NodeVersion { get; set; } = "20";
```

Validation belongs at the public boundary (user-facing API endpoints, external webhooks), not between internal services.