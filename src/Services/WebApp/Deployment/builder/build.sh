#!/bin/bash
set -euo pipefail

mkdir -p /output

REPO_URL="${REPO_URL}"
WORK_DIR="${WORK_DIR:-.}"
OUT_DIR="${OUT_DIR:-dist}"
INSTALL_CMD="${INSTALL_CMD:-npm ci}"
BUILD_CMD="${BUILD_CMD:-npm run build}"
OUTPUT_DIR="/output"

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
echo "Build completed successfully."
