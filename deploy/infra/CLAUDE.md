# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

See the parent [`deploy/CLAUDE.md`](../CLAUDE.md) for Terraform commands, Ansible deployment flow, and overall repository structure.

## What This Directory Manages

Terraform for all production infrastructure:
- **AWS**: EC2 instance (t3.small, Ubuntu), VPC/subnet/security group, IAM (GitHub OIDC role for CI/CD), AWS Secrets Manager secret resources
- **Cloudflare**: DNS records for `mycrocloud.online` (control plane) and `mycrocloud.site` (data plane)
- **Auth0**: SPA client, API resource server, GitHub/Google social connections, M2M client for the build worker

## Modules

- `modules/auth0/` — Auth0 tenant resources (clients, connections, resource server)
- `modules/external_integrations/` — Contains only `slack_app_manifest.json`; no Terraform resources

## Secrets & Variables Convention

- `variables.auto.tfvars` holds all sensitive values (not checked in). Copy from `variables.auto.tfvars.example` to bootstrap.
- `backend.config` holds S3 backend credentials (not checked in; bucket: `075313985331-terraform`, key: `mycrocloud/infra.tfstate`).
- AWS Secrets Manager secret **resources** are declared in `aws_secrets.tf` with the naming prefix `prod/mycrocloud/<service>/<file>`. Secret **values** are managed via the AWS Console, not Terraform.

## Three-File Rule for Secrets

When adding or removing a service secret, update all three places:
1. `aws_secrets.tf` — declare the `aws_secretsmanager_secret` resource
2. `../scripts/deploy.yml` — add to `env_files` or `secret_files` list
3. `../pkg/<service>/` — add the config file that the secret maps to

## GitHub OIDC

The IAM role `github_actions` trusts only `repo:mycrocloud/mycrocloud:ref:refs/heads/main`. If the repo or branch changes, update the `Condition` block in `main.tf`.

## Key Outputs

After `terraform apply`, useful outputs:
- `instance_ip` — public IP wired to Cloudflare DNS
- `auth0_web_client_id`, `auth0_api_identifier`, `auth0_build_worker_client_id` — values needed in service `.env` files
- `aws_iam_role_github_actions_arn` — ARN for the GitHub Actions OIDC role (set as repo secret `AWS_ROLE_ARN`)
