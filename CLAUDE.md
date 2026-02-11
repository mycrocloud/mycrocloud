# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

### Backend (.NET 8, C# preview)
```bash
# Build the full solution
dotnet build src/Services/WebApp/WebApp.sln

# Run the API (control plane)
dotnet run --project src/Services/WebApp/Api

# Run the Gateway (data plane)
dotnet run --project src/Services/WebApp/WebApp.Gateway

# Run migration tests (requires live PostgreSQL)
dotnet test src/Services/WebApp/Api.MigrationTest

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project src/Services/WebApp/Api.Migrations

# Lint frontend
npm run lint --prefix src/Web
```

### Frontend (React 19 + Vite + TypeScript)
```bash
# Main web UI
npm run dev --prefix src/Web        # Dev server
npm run build --prefix src/Web      # Production build (tsc + vite)

# Code editor widget
npm run dev --prefix src/WebEditor
npm run build --prefix src/WebEditor
```

### Docker Compose (dev)
```bash
docker compose -f src/compose.yml up    # web + web-editor only
```

Run services in WebApp after editing code (must run from the WebApp directory so compose.override.yml is picked up).
```bash
cd src/Services/WebApp && docker compose build && docker compose up -d
``` 

## Architecture Overview

**MycroCloud** is a platform for deploying managed web apps (API, SPA, or Fullstack). It has a **dual-plane** design:

- **Control Plane** (`Api` + `Web` + `WebEditor`): Users manage apps, routes, builds, deployments, and integrations
- **Data Plane** (`WebApp.Gateway`): Serves deployed user apps — routes requests to static content, SPA files (from S3/disk), or executes JS functions via Docker containers

Both planes share `Api.Domain` and `Api.Infrastructure` libraries.

### Service Topology (Production)
```
Nginx LB ─┬─ web (React SPA)
           ├─ web-editor (Monaco editor, iframe-embedded)
           ├─ api (ASP.NET Core control plane)
           └─ gateway (ASP.NET Core data plane, caches app specs in Redis)

Deployment subsystem (separate compose):
  worker (Go) ── listens RabbitMQ "job_queue" ── spawns builder containers
  builder (Node.js) ── git clone + npm build + zip artifacts
  fluentd ── aggregates logs → Elasticsearch
```

### Key Project Dependencies
```
Api              → Api.Domain, Api.Infrastructure
WebApp.Gateway   → Api.Domain, Api.Infrastructure
Api.Migrations   → Api.Infrastructure
Api.Infrastructure contains: AppDbContext (EF Core), repositories, storage providers
Api.Domain contains: entities, repository interfaces, domain services
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
`window.CONFIG` injected at runtime via Nginx (allows single Docker image across environments). Falls back to `VITE_*` env vars for local dev. See `src/Web/src/config.ts`.

### CI/CD
GitHub Actions per-service (path-filtered pushes to `main`). Reusable `_build-deploy.yml` workflow: build Docker image → push to `ghcr.io` → SSH + Ansible playbook → `docker compose up`. Secrets from AWS Secrets Manager.

### Documentation project
The document project lives in other repository:
```
../mycrocloud-docs
```