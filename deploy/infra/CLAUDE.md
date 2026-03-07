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
## Key Outputs

After `terraform apply`, useful outputs:
- `instance_ip` — ConoHa VPS public IP wired to Cloudflare DNS
- `auth0_web_client_id`, `auth0_api_identifier`, `auth0_build_worker_client_id` — values needed in service `.env` files
