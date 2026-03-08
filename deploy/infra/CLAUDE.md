# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

See the parent [`deploy/CLAUDE.md`](../CLAUDE.md) for Terraform commands, Ansible deployment flow, and overall repository structure.

## What This Directory Manages

Terraform for all production infrastructure:
- **ConoHa VPS**: Server instance
- **Cloudflare**: DNS records for `mycrocloud.online` (control plane) and `mycrocloud.site` (data plane)
- **Auth0**: SPA client, API resource server, GitHub/Google social connections, M2M client for the build worker

## Modules

- `modules/auth0/` — Auth0 tenant resources (clients, connections, resource server)
- `modules/external_integrations/` — Contains only `slack_app_manifest.json`; no Terraform resources

## Secrets & Variables Convention

- `variables.auto.tfvars` holds all sensitive values (not checked in). Copy from `variables.auto.tfvars.example` to bootstrap.
- `backend.config` holds S3 backend credentials (not checked in; bucket: `075313985331-terraform`, key: `mycrocloud/infra.tfstate`).

## Bitwarden Secrets (`secrets.tf`)

Application secrets are provisioned in Bitwarden Secrets Manager via the `bitwarden-secrets_secret` resource. Secret keys follow these conventions:

- **Naming**: `snake_case` (e.g., `db_connection_string`, not `DB_CONNECTION_STRING` or `ConnectionStrings__DefaultConnection`)
- **Namespacing**: `<service>/<key>` (e.g., `api/db_connection_string`, `monitoring/alloy/grafana_cloud_api_key`)
- **Nested paths**: Use `/` for sub-paths like certs (e.g., `lb/certs/mycrocloud.online.key`)

Only store genuinely sensitive values (passwords, API keys, connection strings, private keys). Public URLs, usernames, and non-sensitive config are hardcoded directly in the Jinja2 config templates under `services/`.

When adding or removing a secret, update `secrets.tf`, `scripts/deploy.yml`, and the service's `.j2` template together.

## Key Outputs

After `terraform apply`, useful outputs:
- `instance_ip` — ConoHa VPS public IP wired to Cloudflare DNS
- `auth0_web_client_id`, `auth0_api_identifier`, `auth0_build_worker_client_id` — values needed in service config templates
