# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

This is the **deployment and infrastructure** repository for MycroCloud. It contains:
- **Terraform** configurations for provisioning AWS + Cloudflare infrastructure
- **Ansible** playbooks for deploying services to servers
- **Docker Compose** definitions and config files for production services

This repo does NOT contain application source code — that lives in the main `mycrocloud` repository.

## Repository Structure

```
infra/              Terraform (AWS EC2, VPC, Cloudflare DNS, Auth0, Secrets Manager, GitHub OIDC)
scripts/            Ansible playbooks + inventory
pkg/                Production compose files, service configs, nginx templates, monitoring
  compose.yml       Root compose (includes webapp/ and monitoring/ composes)
  lb/               Nginx load balancer config + SSL certs + vhost templates
  api/              API service config (appsettings.json, .env.j2)
  web/              Web frontend config (.env.j2)
  dbmigrator/       DB migration runner config
  webapp/
    compose.yml     Gateway + spa_worker services
    gateway/        Gateway service config
    spa/worker/     SPA deployment worker config
  monitoring/
    compose.yml     Seq, Prometheus, node_exporter, nginx_exporter
```

Service paths in `pkg/` mirror `src/pkg/` in the main repo, except `dbmigrator` (source is `src/pkg/api/Api.Migrations`).

Each service may have config files such as `appsettings.json` (checked in, mounted read-only into containers) and `.env.j2` templates (rendered with secrets at deploy time). Secret file paths under `pkg/` match their AWS Secrets Manager names with the `prod/mycrocloud/` prefix — e.g., the secret `prod/mycrocloud/api/.env` is written to `pkg/api/.env` on the server. The corresponding secret resources are defined in `infra/aws_secrets.tf`. When adding or removing a service's `.env` or secret file, update all three places: `pkg/` config files, `scripts/deploy.yml` secret lists, and `infra/aws_secrets.tf`.

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

- **all**: lb, api, web, db_migrator, gateway, spa_worker, seq, prometheus, node_exporter, nginx_exporter
- **core**: lb, api, web, db_migrator
- **webapp**: gateway, spa_worker
- **monitoring**: seq, prometheus, node_exporter, nginx_exporter

## Deployment Flow

1. `deploy.yml` fetches secrets from AWS Secrets Manager (env files, certs, keys)
2. Syncs `pkg/` directory to server at `/opt/mycrocloud`
3. Writes secrets to `.env` files on server
4. Renders Jinja2 templates (`.j2` files → final config)
5. Runs `docker compose up` with image pull
6. Waits for health checks
7. Reloads nginx if api/gateway/web services were updated

## Secrets Management

Secrets are stored in AWS Secrets Manager under the `prod/mycrocloud/` prefix. The deploy playbook fetches them automatically. Secret categories:
- **env_files**: `.env` files for each service (rendered from Secrets Manager values)
- **secret_files**: SSL certs, GitHub App PEM keys
- **template_secrets**: Values injected into `.j2` templates (e.g., Prometheus config with Grafana Cloud credentials)

## Docker Compose Topology

The root `pkg/compose.yml` includes `webapp/compose.yml` and `monitoring/compose.yml`. All services pull pre-built images from `ghcr.io/mycrocloud/`. The gateway container mounts the Docker socket (for spawning function invoker containers) and `/srv/function-data`.

## Nginx Load Balancer

Nginx uses envsubst templates in `pkg/lb/templates/conf.d/` — domain names are injected via `CONTROL_PLANE_DOMAIN` and `DATA_PLANE_DOMAIN` environment variables. Includes Cloudflare IP allowlists and shared SSL configuration.
