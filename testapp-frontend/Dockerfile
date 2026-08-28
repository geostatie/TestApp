# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build stage
# ---------------------------------------------------------------------------
FROM node:24-alpine AS build

WORKDIR /src

# Copy manifests first so the dependency layer is cached until they change.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------------------------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------------------------
# nginx-unprivileged already runs as uid 101 and listens on 8080, which is what
# Container Apps expects. The stock nginx image runs as root on port 80.
FROM nginxinc/nginx-unprivileged:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf

# The Angular application builder emits to dist/<project>/browser.
# Owned by uid 101 so the entrypoint can rewrite config.json at startup.
COPY --from=build --chown=101:101 /src/dist/testapp-frontend/browser /usr/share/nginx/html

# Scripts in /docker-entrypoint.d are run by the nginx image before nginx
# starts, so there is no need to override ENTRYPOINT or CMD.
COPY --chmod=0755 docker-entrypoint.sh /docker-entrypoint.d/40-write-runtime-config.sh

EXPOSE 8080
