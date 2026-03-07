# Copilot Instructions for MycroCloud

## Project Overview
- **MycroCloud** is a cloud platform with a clear split between the **Control Plane** (API, user/app management) and **Data Plane** (Gateway, user app serving).
- The codebase is polyrepo-style: application code is in `src/`, infrastructure/deployment in `deploy/`.
- Key backend: ASP.NET Core (C# 8, preview features), frontend: React 19 + Vite + TypeScript, plus Go for deployment workers.

## Solution Structure & Key Components
- `src/services/api/Api.sln`: Control Plane API (user/app/build/deployment management)
  - `Api`: ASP.NET Core Web API
  - `Api.Domain`: Entities, enums, repository interfaces, domain services
  - `Api.Infrastructure`: EF Core DbContext, repositories
  - `Api.Migrations`: EF Core migrations
  - `Api.MigrationTest`: Integration tests (needs live PostgreSQL)
- `src/services/web`: Frontend (React, Vite, Tailwind)
- `src/services/webapp`: Data Plane (Gateway, SPA worker, Go deployment worker)
- `deploy/`: Terraform (infra), Ansible (deploy), Docker Compose (prod config)

## Developer Workflows
- **Build API**: `dotnet build src/services/api/Api.sln`
- **Run API**: `dotnet run --project src/services/api/Api`
- **Run Gateway**: `dotnet run --project src/services/webapp/gateway`
- **Frontend Dev**: `npm run dev --prefix src/services/web`
- **Frontend Build**: `npm run build --prefix src/services/web`
- **Lint**: `npm run lint --prefix src/services/web`
- **Go Worker**: `cd src/services/webapp/spa/worker && go build ./...`
- **Migrations**: `dotnet ef migrations add <Name> --project src/services/api/Api.Migrations`
- **Integration Tests**: `dotnet test src/services/api/Api.MigrationTest`

## Patterns & Conventions
- **Auth**: Multi-scheme (Auth0 JWT, API Key, Slack) via `MultiAuthSchemes` policy. See `Api/Authentications/`.
- **Authorization**: `AppOwnerActionFilter` for app ownership; opt-out with `[DisableAppOwnerActionFilter]`.
- **Secrets/Config**: Service config files in `deploy/pkg/`, secrets managed via AWS Secrets Manager, paths must match infra definitions.
- **Frontend**: Uses Vite, Tailwind, TypeScript. See `src/services/web/` for conventions.
- **API Security**: FunctionInvoker strips sensitive headers, enforces SSRF protections, and tracks recursion via `X-MycroCloud-Depth`.

## Integration & Infra
- **Terraform**: `deploy/infra/` for AWS, Cloudflare, Auth0, etc.
- **Ansible**: `deploy/scripts/` for deployment playbooks.
- **Docker Compose**: `deploy/pkg/compose.yml` (includes all services for prod).
- **Secrets**: Update in three places: `pkg/` config, `scripts/deploy.yml`, `infra/aws_secrets.tf`.

## Examples
- To add a new API endpoint: implement in `Api/Controllers/`, add domain logic in `Api.Domain/`, update `Api.Infrastructure/` if persistence needed.
- To add a new frontend page: add to `src/services/web/src/pages/`, update routes in `src/services/web/src/App.tsx`.
- To add a new secret: define in `infra/aws_secrets.tf`, reference in `pkg/`, and list in `scripts/deploy.yml`.

## References
- See `src/services/api/AGENTS.md` and `CLAUDE.md` for more details on architecture and workflows.
- For deployment/infrastructure, see `deploy/CLAUDE.md`.
