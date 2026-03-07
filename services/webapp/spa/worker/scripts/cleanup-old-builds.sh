#!/bin/bash
# Cleanup old build outputs that are older than MAX_AGE_DAYS
# Run via cronjob: 0 2 * * * /path/to/cleanup-old-builds.sh

set -euo pipefail

BUILD_OUTPUT_DIR="${BUILD_OUTPUT_DIR:-/tmp/build-outputs}"
MAX_AGE_DAYS="${MAX_AGE_DAYS:-1}"

if [ ! -d "$BUILD_OUTPUT_DIR" ]; then
    echo "Build output directory does not exist: $BUILD_OUTPUT_DIR"
    exit 0
fi

echo "Cleaning up build outputs older than $MAX_AGE_DAYS days in $BUILD_OUTPUT_DIR"

# Find and delete directories older than MAX_AGE_DAYS
deleted_count=0
while IFS= read -r -d '' dir; do
    echo "Deleting: $dir"
    rm -rf "$dir"
    ((deleted_count++)) || true
done < <(find "$BUILD_OUTPUT_DIR" -mindepth 1 -maxdepth 1 -type d -mtime +$MAX_AGE_DAYS -print0)

echo "Deleted $deleted_count old build directories"

# Also clean up any orphaned zip files directly in the output dir
find "$BUILD_OUTPUT_DIR" -maxdepth 1 -name "*.zip" -mtime +$MAX_AGE_DAYS -delete 2>/dev/null || true

echo "Cleanup completed"
