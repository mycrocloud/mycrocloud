#!/bin/bash
set -euo pipefail
shopt -s nullglob

# SCRIPT_VERSION: 20260208-1825 (ONLY_ZIP_MODE)

mkdir -p /output

echo "------ Build Started (v20260208-1825) ------"
echo "WorkDir: ${WORK_DIR:-.}"
echo "OutDir: ${OUT_DIR:-dist}"

REPO_URL="${REPO_URL}"
WORK_DIR="${WORK_DIR:-.}"
OUT_DIR="${OUT_DIR:-dist}"
INSTALL_CMD="${INSTALL_CMD:-npm ci}"
BUILD_CMD="${BUILD_CMD:-npm run build}"
NODE_VERSION="${NODE_VERSION:-}"
ENV_VARS_JSON="${ENV_VARS:-}"
OUTPUT_DIR="/output"

# --- Setup Node version if specified ---
if [ -n "$NODE_VERSION" ]; then
    echo "Setting up Node.js version: $NODE_VERSION"
    export NVM_DIR="${NVM_DIR:-/home/builder/.nvm}"
    [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
    nvm install "$NODE_VERSION" > /dev/null
    nvm use "$NODE_VERSION" > /dev/null
    echo "Using Node.js $(node --version)"
fi

# --- Export environment variables if provided ---
if [ -n "$ENV_VARS_JSON" ]; then
    for key in $(echo "$ENV_VARS_JSON" | jq -r 'keys[]'); do
        value=$(echo "$ENV_VARS_JSON" | jq -r --arg k "$key" '.[$k]')
        export "$key"="$value"
    done
fi

# --- Main build flow ---
echo "Cloning repository..."
git clone --depth 1 "$REPO_URL" repo
cd repo/"$WORK_DIR"

echo "Installing dependencies..."
eval "$INSTALL_CMD"

echo "Building project..."
eval "$BUILD_CMD"

if [ ! -d "$OUT_DIR" ]; then
    echo "❌ Error: Output directory '$OUT_DIR' was not created."
    ls -la
    exit 1
fi

# ZIP output directly to the host-mounted volume
echo "Creating zip artifact directly in host mount..."
cd "$OUT_DIR"
# Remove existing zip and raw files in output dir (just in case of reuse)
rm -rf "$OUTPUT_DIR"/*
# Zip contents of current dir (.) into the host-mounted path
zip -qr "$OUTPUT_DIR/${OUT_DIR}.zip" .

echo "------ Build Successful ------"
echo "Artifact created: $OUTPUT_DIR/${OUT_DIR}.zip"
ls -la "$OUTPUT_DIR"
du -sh "$OUTPUT_DIR/${OUT_DIR}.zip"
