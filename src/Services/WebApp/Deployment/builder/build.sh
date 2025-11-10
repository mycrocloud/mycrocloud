#!/bin/bash
set -euo pipefail

# --- Logging setup ---
#LOG_FILE="/output/build_$(date +%Y%m%d_%H%M%S).log"
mkdir -p /output
#exec > >(tee -a "$LOG_FILE") 2>&1

# --- Helper functions ---
timestamp() { date +"%Y-%m-%d %H:%M:%S"; }
log() { echo -e "\033[1;32m[$(timestamp)] [INFO]\033[0m $*"; }
error() { echo -e "\033[1;31m[$(timestamp)] [ERROR]\033[0m $*" >&2; }

# --- Env variables (đã được worker đảm bảo hợp lệ) ---
REPO_URL="${REPO_URL}"
WORK_DIR="${WORK_DIR:-.}"
OUT_DIR="${OUT_DIR:-dist}"
INSTALL_CMD="${INSTALL_CMD:-npm ci}"
BUILD_CMD="${BUILD_CMD:-npm run build}"
OUTPUT_DIR="/output"

# --- Main build flow ---
#log "🔧 Logging to $LOG_FILE"
log "Cloning repository from $REPO_URL..."
git clone --depth 1 "$REPO_URL" repo
cd repo/"$WORK_DIR"

log "Installing dependencies using: $INSTALL_CMD"
eval "$INSTALL_CMD"

log "Building project using: $BUILD_CMD"
eval "$BUILD_CMD"

if [ -d "$OUT_DIR" ]; then
  mkdir -p "$OUTPUT_DIR"
  cp -r "$OUT_DIR"/* "$OUTPUT_DIR"/
  log "✅ Build completed successfully."
else
  error "Output directory '$OUT_DIR' not found."
  exit 1
fi
