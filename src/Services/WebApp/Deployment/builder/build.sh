#!/bin/bash
set -euo pipefail
shopt -s nullglob

# SCRIPT_VERSION: 20260212-1000 (ENHANCED_LOGGING)

mkdir -p /output

BUILD_START_TIME=$(date +%s)

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 Build Started (v20260212-1000)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "WorkDir: ${WORK_DIR:-.}"
echo "OutDir: ${OUT_DIR:-dist}"
echo "Started: $(date -u +%Y-%m-%dT%H:%M:%SZ)"

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
    echo ""
    echo "[1/4] 📦 Setting up Node.js..."
    export NVM_DIR="${NVM_DIR:-/home/builder/.nvm}"
    [ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
    nvm install "$NODE_VERSION" > /dev/null
    nvm use "$NODE_VERSION" > /dev/null
    echo "✓ Node.js $(node --version) | npm $(npm --version)"
else
    echo ""
    echo "[1/4] 📦 Using default Node.js..."
    echo "✓ Node.js $(node --version) | npm $(npm --version)"
fi

# --- Export environment variables if provided ---
if [ -n "$ENV_VARS_JSON" ]; then
    for key in $(echo "$ENV_VARS_JSON" | jq -r 'keys[]'); do
        value=$(echo "$ENV_VARS_JSON" | jq -r --arg k "$key" '.[$k]')
        export "$key"="$value"
    done
fi

# --- Main build flow ---
echo ""
echo "[2/4] 📥 Cloning repository..."
CLONE_START=$(date +%s)
git clone --depth 1 "$REPO_URL" repo
CLONE_END=$(date +%s)
cd repo/"$WORK_DIR"
echo "✓ Repository cloned in $((CLONE_END - CLONE_START))s"

echo ""
echo "[3/4] 📚 Installing dependencies..."
INSTALL_START=$(date +%s)
eval "$INSTALL_CMD"
INSTALL_END=$(date +%s)
echo "✓ Dependencies installed in $((INSTALL_END - INSTALL_START))s"

echo ""
echo "[4/4] 🔨 Building project..."
BUILD_CMD_START=$(date +%s)
eval "$BUILD_CMD"
BUILD_CMD_END=$(date +%s)
echo "✓ Build completed in $((BUILD_CMD_END - BUILD_CMD_START))s"

if [ ! -d "$OUT_DIR" ]; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "❌ Build Failed"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Error: Output directory '$OUT_DIR' was not created"
    echo "Current directory contents:"
    ls -la
    exit 1
fi

# ZIP output directly to the host-mounted volume
echo ""
echo "📦 Creating artifact..."
cd "$OUT_DIR"
# Remove existing zip and raw files in output dir (just in case of reuse)
rm -rf "$OUTPUT_DIR"/*
# Zip contents of current dir (.) into the host-mounted path
zip -qr "$OUTPUT_DIR/${OUT_DIR}.zip" .

BUILD_END_TIME=$(date +%s)
TOTAL_TIME=$((BUILD_END_TIME - BUILD_START_TIME))

echo ""
echo "✅ Build completed successfully in ${TOTAL_TIME}s"
