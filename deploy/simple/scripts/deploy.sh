#!/bin/bash
set -euo pipefail

# Configuration
USER="${DEPLOY_USER:-ubuntu}"
IP="${DEPLOY_IP:?Error: DEPLOY_IP environment variable not set}"
KEY_PATH="${DEPLOY_KEY:-$HOME/.ssh/id_rsa}"

SERVICE="${1:-}"

# Validate key exists
if [[ ! -f "$KEY_PATH" ]]; then
  echo "Error: SSH key not found at $KEY_PATH" >&2
  exit 1
fi

# Build deployment command
if [[ -n "$SERVICE" ]]; then
  echo "Deploying service: $SERVICE"
  CMD="docker compose down '$SERVICE' && docker compose pull '$SERVICE' && docker compose up -d '$SERVICE'"
else
  echo "Deploying all services"
  CMD="docker compose up -d"
fi

# Deploy
echo "Connecting to $USER@$IP..."
ssh "$USER@$IP" -i "$KEY_PATH" -o ConnectTimeout=10 << EOF
  set -e
  cd app
  $CMD
  echo "Deployment completed successfully"
EOF

echo "Deployment finished"