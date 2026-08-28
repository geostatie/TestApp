#!/bin/sh
#
# Regenerate the runtime configuration from the environment.
#
# This is what lets one image run against any environment: the API URL is never
# compiled into the bundle, it is written here at container start. Runs before
# nginx via the image's /docker-entrypoint.d mechanism.

set -eu

# Fail loudly rather than silently serving a stale or default URL.
: "${API_BASE_URL:?API_BASE_URL must be set (e.g. https://testapp.example.azurecontainerapps.io)}"

CONFIG_PATH=/usr/share/nginx/html/config.json

cat > "$CONFIG_PATH" <<EOF
{
  "apiBaseUrl": "${API_BASE_URL}"
}
EOF

echo "[entrypoint] wrote ${CONFIG_PATH} with apiBaseUrl=${API_BASE_URL}"
