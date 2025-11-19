#!/usr/bin/env bash
set -e
echo "Starting Cloudflare Tunnel..."
cd "$(dirname "$0")"
cloudflared tunnel --config ./config.yml run