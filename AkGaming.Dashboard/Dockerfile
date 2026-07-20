# Build stage (monorepo context expected)
FROM node:22-alpine AS build
WORKDIR /app/AkGaming.Dashboard

COPY AkGaming.Dashboard/package*.json ./
RUN npm ci

COPY AkGaming.Dashboard ./
COPY AkGaming.Core ../AkGaming.Core

RUN npm run build

# Serve with Caddy (static)
FROM caddy:2-alpine

COPY --from=build /app/AkGaming.Dashboard/dist /usr/share/caddy
COPY AkGaming.Dashboard/Caddyfile /etc/caddy/Caddyfile

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1/health || exit 1
