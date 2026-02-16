# MycroCloud Deployment Guide

This directory contains Ansible playbooks for deploying MycroCloud infrastructure and services to AWS.

## Prerequisites

- Python 3.x installed
- AWS account with appropriate permissions
- AWS credentials configured (via `~/.aws/credentials` or environment variables)
- SSH access to target servers
- Required secrets and certificates (see [Secrets Configuration](#secrets-configuration))

## Installation

### 1. Install Ansible (One-time Setup)

Create a virtual environment and install dependencies:

```bash
python3 -m venv .venv && \
source .venv/bin/activate && \
pip install --upgrade pip && \
pip install ansible boto3 botocore
```

## Configuration

### 2. Activate Environment

Before running any playbooks, activate the virtual environment:

```bash
source .venv/bin/activate
```

**macOS Users:** Set this environment variable to prevent fork safety issues:

```bash
export OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES
```

### 3. Configure Inventory

Update [inventory.yml](inventory.yml) with your environment-specific configuration:

- Server hostnames/IPs
- SSH connection details
- Environment variables
- Service configurations

### 4. Initial Setup

Run the setup playbook to prepare the infrastructure:

```bash
ansible-playbook -i inventory.yml setup.yml
```

This configures servers, installs dependencies, and prepares the deployment environment.

## Secrets Configuration

Before deployment, ensure the following secret files are created and populated:

### Required Secret Files

- [ ] `prod/mycrocloud/lb/certs/mycrocloud.online.pem` - SSL/TLS certificate for load balancer
- [ ] `prod/mycrocloud/lb/certs/mycrocloud.site.pem` - SSL/TLS certificate for data plane load balancer
- [ ] `prod/mycrocloud/lb/certs/mycrocloud.online.key` - private key for control plane certificate
- [ ] `prod/mycrocloud/lb/certs/mycrocloud.site.key` - private key for data plane certificate
- [ ] `prod/mycrocloud/Services/WebApp/deployment/.env` - WebApp deployment environment variables
- [ ] `prod/mycrocloud/.env` - Main environment configuration
- [ ] `prod/mycrocloud/Services/WebApp/WebApp.Api/gha-mycrocloud.pem` - GitHub Actions authentication key

### Creating Secrets

1. **SSL Certificates**: Obtain from your certificate provider or use AWS Certificate Manager
2. **Environment Files**: Create `.env` files with required variables for your environment
3. **GitHub Actions Keys**: Generate or obtain from your GitHub repository settings

Store these files securely using AWS Secrets Manager or your preferred secrets management solution.

## Deployment

### Full Deployment (First Run)

Deploy all services:

```bash
ansible-playbook -i inventory.yml deploy.yml
```

### Selective Deployment

Deploy specific services:

```bash
ansible-playbook -i inventory.yml deploy.yml -e "services=['web','api']"
```

Deploy by group:

```bash
ansible-playbook -i inventory.yml deploy.yml -e "service_group=core"
```

Default group is `all` when `service_group` and `services` are not set.

Service names use `underscore` style:
- `db_migrator`
- `spa_worker`
- `nginx_exporter`

## Verification

After deployment, verify the services are running:

```bash
# Check service status
ansible all -i inventory.yml -m shell -a "docker ps"

# Test web endpoint
curl https://mycrocloud.online/health
```

## Troubleshooting

### Common Issues

**Ansible connection errors:**
- Verify SSH access to target servers
- Check inventory.yml for correct hostnames/IPs
- Ensure SSH keys are properly configured

**Permission errors:**
- Verify AWS credentials have necessary permissions
- Check file permissions on secret files

**macOS fork safety warnings:**
- Ensure `OBJC_DISABLE_INITIALIZE_FORK_SAFETY=YES` is set

### Logs

Check Ansible verbose output for detailed error information:

```bash
ansible-playbook -i inventory.yml deploy.yml -vvv
```

## Rollback

To rollback to a previous deployment:

```bash
# Specify the version/tag to rollback to
ansible-playbook -i inventory.yml deploy.yml -e "version=<previous-version>"
```

## Maintenance

### Updating Dependencies

```bash
source .venv/bin/activate
pip install --upgrade ansible boto3 botocore
```

### Deactivating Environment

When finished:

```bash
deactivate
```
