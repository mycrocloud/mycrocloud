# Deployment

## GitHub Actions Configuration

### Repository Variables

| Variable | Description |
|---|---|
| `SERVER_HOST` | Server hostname or IP address for SSH deployment |
| `AWS_IAM_ROLE_GITHUB_ACTIONS_ARN` | AWS IAM role ARN assumed by GitHub Actions (OIDC) |
| `AWS_REGION` | AWS region (e.g. `ap-northeast-1`) |

### Repository Secrets

| Secret | Description |
|---|---|
| `SSH_PRIVATE_KEY` | SSH private key for connecting to the server |
| `CR_PAT` | GitHub Personal Access Token for container registry (`ghcr.io`) |
