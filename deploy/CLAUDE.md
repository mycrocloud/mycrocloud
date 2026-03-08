# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

This is the **deployment and infrastructure** repository for MycroCloud. It contains:
- **Terraform** configurations for provisioning ConoHa VPS + Cloudflare infrastructure
- **Ansible** playbooks for deploying services to servers
- **Docker Compose** definitions and config files for production services

This repo does NOT contain application source code — that lives in the main `mycrocloud` repository.

## Repository Structure

```
infra/              Terraform (ConoHa VPS, Cloudflare DNS, Auth0, Bitwarden Secrets, GitHub OIDC)
scripts/            Ansible playbooks + inventory
services/           Production compose files, service configs, nginx templates, monitoring
  compose.yml       Root compose (includes webapp/ and monitoring/ composes)
  lb/               Nginx load balancer config + SSL certs + vhost templates
  api/              appsettings.json.j2
  dbmigrator/       appsettings.json.j2
  webapp/
    compose.yml     Gateway + spa_build_worker services
    gateway/        appsettings.json.j2
    spa/build-worker/  .conf.j2
  monitoring/
    compose.yml     Alloy, Prometheus, node_exporter
    alloy/          config.alloy.j2
    prometheus/     prometheus.yml.j2
```

Service paths in `services/` mirror `services/` in the main repo, except `dbmigrator` (source is `services/api/Api.Migrations`).

Each service has a single Jinja2 config template (`.j2` extension) that contains both non-secret config values (hardcoded) and secret values (injected via `{{ secrets.<key> }}`). There are no separate `.env.j2` files — everything is in one file per service.

## Commands

### Terraform (Infrastructure)

```bash
cd infra
terraform init -backend-config=backend.config
terraform plan
terraform apply
```

Requires `backend.config` and `variables.auto.tfvars` (see `.example` files).

### Ansible (Deployment)

```bash
# One-time setup
cd scripts
python3 -m venv .venv && source .venv/bin/activate
pip install ansible boto3 botocore

# macOS requires:
export OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES

# Initial server setup (Docker, directories)
ansible-playbook -i inventory.yml setup.yml

# Deploy all services
ansible-playbook -i inventory.yml deploy.yml

# Deploy a service group
ansible-playbook -i inventory.yml deploy.yml -e "service_group=core"

# Deploy specific services
ansible-playbook -i inventory.yml deploy.yml -e "services=['web','api']"
```

Inventory uses environment variables: `ANSIBLE_HOST`, `ANSIBLE_USER`, `ANSIBLE_SSH_PRIVATE_KEY_FILE`.

### Service Groups

- **all**: lb, api, web, db_migrator, gateway, spa_build_worker, alloy, prometheus, node_exporter
- **core**: lb, api, web, db_migrator
- **webapp**: gateway, spa_build_worker
- **monitoring**: alloy, prometheus, node_exporter

## Deployment Flow

1. `deploy.yml` fetches secrets from Bitwarden Secrets Manager
2. Syncs `services/` directory to server at `/opt/mycrocloud`
3. Renders Jinja2 templates (`.j2` files → final config files) with injected secrets
4. Copies plaintext secret files (SSL certs, PEM keys) to server
5. Runs `docker compose up` with image pull
6. Waits for health checks
7. Reloads nginx if api/gateway/web services were updated

## Secrets Management

Secrets are stored in **Bitwarden Secrets Manager** and provisioned via Terraform (`infra/secrets.tf`). The deploy playbook fetches them automatically at deploy time.

### Secret Key Naming

Secret keys use `snake_case` and are namespaced by service path, e.g.:
- `api/db_connection_string`
- `monitoring/alloy/grafana_cloud_api_key`
- `monitoring/prometheus/grafana_api_key`
- `lb/certs/mycrocloud.online.key`

### What IS a secret

Only values that are genuinely sensitive: passwords, API keys, connection strings, private keys.

### What is NOT a secret

Public URLs, usernames, service addresses, and other non-sensitive values are hardcoded directly in the `.j2` config templates. Do not store these in Bitwarden.

Examples of hardcoded (non-secret) values in templates:
- Grafana Loki URL and username in `monitoring/alloy/config.alloy.j2`
- Prometheus remote-write URL and username in `monitoring/prometheus/prometheus.yml.j2`

### Adding/Removing a Secret

When adding or removing a secret, update **all three** places:
1. `infra/secrets.tf` — add/remove from the appropriate `locals` list
2. `scripts/deploy.yml` — add/remove from `service_j2_template_files` mappings
3. The service's `.j2` config template — add/remove the `{{ secrets.<key> }}` reference

## Docker Compose Topology

The root `services/compose.yml` includes `webapp/compose.yml` and `monitoring/compose.yml`. All services pull pre-built images from `ghcr.io/mycrocloud/`. The gateway container mounts the Docker socket (for spawning function invoker containers) and `/srv/function-data`.

Config files are volume-mounted into containers (read-only) — services do not use `env_file`.

## Nginx Load Balancer

Nginx uses envsubst templates in `services/lb/templates/conf.d/` — domain names are injected via `CONTROL_PLANE_DOMAIN` and `DATA_PLANE_DOMAIN` environment variables. Includes Cloudflare IP allowlists and shared SSL configuration.
