#!/bin/sh

REPO_URL=${REPO_URL:-"https://default-repo-url.git"}
WORK_DIR=${WORK_DIR:-"."}
OUT_DIR=${OUT_DIR:-"dist"}
INSTALL_CMD=${INSTALL_CMD:-"npm install"}
BUILD_CMD=${BUILD_CMD:-"npm run build"}

if [ -z "$REPO_URL" ]; then
  echo "Error: REPO_URL is not set."
  exit 1
fi

git clone "$REPO_URL" .

cd "$WORK_DIR" || exit

$INSTALL_CMD

$BUILD_CMD

mkdir -p /output

mv "$OUT_DIR"/* /output/
