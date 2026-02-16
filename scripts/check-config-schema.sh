#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(git rev-parse --show-toplevel)"
cd "$ROOT_DIR"

failures=0

extract_env_keys() {
  local file="$1"
  awk '
    {
      line=$0
      sub(/^[[:space:]]+/, "", line)
      if (line == "" || line ~ /^#/) {
        next
      }
      sub(/^export[[:space:]]+/, "", line)
      split(line, parts, "=")
      key=parts[1]
      gsub(/[[:space:]]+$/, "", key)
      if (key ~ /^[A-Za-z_][A-Za-z0-9_]*$/) {
        print key
      }
    }
  ' "$file" | sort -u
}

extract_json_paths() {
  local file="$1"
  jq -r '
    paths(scalars) | map(tostring) | join(".")
  ' "$file" | sort -u
}

compare_schema() {
  local source_file="$1"
  local deploy_file="$2"
  local schema_type="$3"

  local source_keys
  local deploy_keys
  local missing_in_deploy
  local extra_in_deploy

  if [[ "$schema_type" == "env" ]]; then
    source_keys="$(extract_env_keys "$source_file")"
    deploy_keys="$(extract_env_keys "$deploy_file")"
  elif [[ "$schema_type" == "json" ]]; then
    source_keys="$(extract_json_paths "$source_file")"
    deploy_keys="$(extract_json_paths "$deploy_file")"
  else
    echo "Unsupported schema type: ${schema_type}"
    failures=$((failures + 1))
    return
  fi

  missing_in_deploy="$(comm -23 <(printf '%s\n' "$source_keys") <(printf '%s\n' "$deploy_keys") || true)"
  extra_in_deploy="$(comm -13 <(printf '%s\n' "$source_keys") <(printf '%s\n' "$deploy_keys") || true)"

  if [[ -n "$missing_in_deploy" || -n "$extra_in_deploy" ]]; then
    echo "Schema mismatch:"
    echo "  type: ${schema_type}"
    echo "  source: ${source_file}"
    echo "  deploy: ${deploy_file}"

    if [[ -n "$missing_in_deploy" ]]; then
      echo "  Missing in deploy:"
      while IFS= read -r key; do
        [[ -n "$key" ]] && echo "    - $key"
      done <<< "$missing_in_deploy"
    fi

    if [[ -n "$extra_in_deploy" ]]; then
      echo "  Extra in deploy:"
      while IFS= read -r key; do
        [[ -n "$key" ]] && echo "    - $key"
      done <<< "$extra_in_deploy"
    fi

    failures=$((failures + 1))
  fi
}

check_pair() {
  local source_file="$1"
  local deploy_file="$2"
  local schema_type="$3"

  if [[ ! -f "$source_file" ]]; then
    echo "Missing source file: ${source_file}"
    failures=$((failures + 1))
    return
  fi

  if [[ ! -f "$deploy_file" ]]; then
    echo "Missing deploy file: ${deploy_file}"
    failures=$((failures + 1))
    return
  fi

  compare_schema "$source_file" "$deploy_file" "$schema_type"
}

# Explicit temporary check list: only appsettings.json and .env.example schemas.
while read -r source_file deploy_file schema_type; do
  [[ -z "${source_file:-}" ]] && continue
  check_pair "$source_file" "$deploy_file" "$schema_type"
done <<'EOF'
src/pkg/api/Api/.env.example deploy/pkg/api/.env.j2 env
src/pkg/api/Api.Migrations/.env.example deploy/pkg/dbmigrator/.env.j2 env
src/pkg/webapp/gateway/.env.example deploy/pkg/webapp/gateway/.env.j2 env
src/pkg/webapp/spa/worker/.env.example deploy/pkg/webapp/spa/worker/.env.j2 env
src/pkg/api/Api/appsettings.json deploy/pkg/api/appsettings.json json
src/pkg/api/Api.Migrations/appsettings.json deploy/pkg/dbmigrator/appsettings.json json
src/pkg/webapp/gateway/appsettings.json deploy/pkg/webapp/gateway/appsettings.json json
EOF

if [[ "$failures" -ne 0 ]]; then
  echo
  echo "Config schema check failed with ${failures} issue(s)."
  exit 1
fi

echo "Config schema check passed."
