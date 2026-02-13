# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Backend (.NET 8, C# preview)
```bash
# Build API solution (control plane)
dotnet build src/pkg/api/Api.sln

# Build WebApp solution (data plane + shared libs)
dotnet build src/pkg/webapp/WebApp.sln

# Run the API (control plane)
dotnet run --project src/pkg/api/Api

# Run the Gateway (data plane)
dotnet run --project src/pkg/webapp/gateway

# Run migration tests (requires live PostgreSQL)
dotnet test src/pkg/api/Api.MigrationTest

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project src/pkg/api/Api.Migrations
```

### Frontend (React 19 + Vite + TypeScript)
```bash
npm run dev --prefix src/pkg/web        # Dev server
npm run build --prefix src/pkg/web      # Production build (tsc + vite)
npm run lint --prefix src/pkg/web       # ESLint
```

### Go Deployment Worker
```bash
cd src/pkg/webapp/spa/worker && go build ./...
```

### Docker Compose (dev)
```bash
# Frontend only
docker compose -f src/compose.yml up

# Backend services (run from webapp dir so compose.override.yml is picked up)
cd src/pkg/webapp && docker compose build && docker compose up -d

# Deployment subsystem (worker + builder)
cd src/pkg/webapp/spa && docker compose up -d
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
  fluentd ── aggregates logs → Elasticsearch
```

### Project Dependencies
```
src/pkg/api/
  Api              → Api.Domain, Api.Infrastructure
  Api.Migrations   → Api.Infrastructure
  Api.MigrationTest

src/pkg/webapp/
  WebApp.Gateway           (self-contained data plane service)
  WebApp.FunctionInvoker   (standalone console app for JS execution)

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
PostgreSQL with EF Core 8 (Npgsql). Uses JSONB columns, owned JSON entities for settings, TPH discriminator for `Deployment` → `SpaDeployment`/`ApiDeployment`. `AppDbContext` auto-stamps `CreatedAt`/`UpdatedAt`/`Version` in `SaveChangesAsync`.

Migrations live in a separate `Api.Migrations` project. Design-time factory in `AppDbContextFactory`.

### Frontend Config Injection
`window.CONFIG` injected at runtime via Nginx (allows single Docker image across environments). Falls back to `VITE_*` env vars for local dev. See `src/pkg/web/src/config.ts`.

### CI/CD
GitHub Actions per-service (path-filtered pushes to `main`). Reusable `_build-deploy.yml` workflow: build Docker image → push to `ghcr.io` → SSH + Ansible playbook → `docker compose up`. Secrets from AWS Secrets Manager.

### Documentation project
The documentation project lives in a separate repository:
```
../mycrocloud-docs
```
