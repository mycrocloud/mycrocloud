#!/bin/bash
set -euo pipefail

mkdir -p /output

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
    export NVM_DIR="/root/.nvm"
    [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
    nvm install "$NODE_VERSION"
    nvm use "$NODE_VERSION"
    echo "Using Node.js $(node --version)"
else
    echo "Using default Node.js $(node --version)"
fi

# --- Export environment variables if provided ---
if [ -n "$ENV_VARS_JSON" ]; then
    echo "Setting up environment variables..."
    # Parse JSON and export each key-value pair
    for key in $(echo "$ENV_VARS_JSON" | jq -r 'keys[]'); do
        value=$(echo "$ENV_VARS_JSON" | jq -r --arg k "$key" '.[$k]')
        export "$key"="$value"
        echo "  - $key=***"
    done
fi

# --- Main build flow ---
echo "Cloning repository from $REPO_URL..."
git clone --depth 1 "$REPO_URL" repo
cd repo/"$WORK_DIR"

echo "Installing dependencies using: $INSTALL_CMD"
eval "$INSTALL_CMD"

echo "Building project using: $BUILD_CMD"
eval "$BUILD_CMD"

mkdir -p "$OUTPUT_DIR"
cp -r "$OUT_DIR"/* "$OUTPUT_DIR"/

# ZIP output for efficient upload
echo "Creating ${OUT_DIR}.zip..."
cd "$OUTPUT_DIR"
zip -qr "/output/${OUT_DIR}.zip" .
echo "Build completed successfully."
