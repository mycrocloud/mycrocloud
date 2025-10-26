#!/bin/sh

CONFIG_PATH=/usr/share/nginx/html/_config.js
ENV_FILE=/usr/share/nginx/html/.env

echo "window.CONFIG = {" > $CONFIG_PATH
while IFS='=' read -r key value; do
  [ -z "$key" ] && continue
  case "$key" in \#*) continue;; esac
  echo "  \"$key\": \"$value\"," >> $CONFIG_PATH
done < "$ENV_FILE"
echo "};" >> $CONFIG_PATH

echo "Generated runtime config from $ENV_FILE"
cat $CONFIG_PATH
exec nginx -g "daemon off;"