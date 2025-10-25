#!/bin/bash
set -euo pipefail

# Configuration
USER="${DEPLOY_USER:-ubuntu}"
IP="${DEPLOY_IP:?Error: DEPLOY_IP environment variable not set}"
KEY_PATH="${DEPLOY_KEY:-$HOME/.ssh/id_rsa}"

if [[ ! -f "$KEY_PATH" ]]; then
  echo "Error: SSH key not found at $KEY_PATH" >&2
  exit 1
fi

# Add host key if needed
if ! ssh-keygen -F "$IP" > /dev/null 2>&1; then
  echo "Adding $IP to known_hosts..."
  ssh-keyscan -H "$IP" >> ~/.ssh/known_hosts 2>/dev/null
fi

echo "Syncing app directory to $USER@$IP..."
rsync -avz --delete \
  -e "ssh -i $KEY_PATH" \
  app/ "$USER@$IP":~/app/

echo "Sync completed successfully"