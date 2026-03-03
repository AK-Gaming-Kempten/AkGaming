# Build stage (monorepo context expected)
FROM node:22-alpine AS build
WORKDIR /app/AkGaming.Website

COPY AkGaming.Website/package*.json ./
RUN npm ci

COPY AkGaming.Website ./
COPY AkGaming.Core ../AkGaming.Core

RUN npm run build

# Serve with Caddy (static)
FROM caddy:2-alpine

COPY --from=build /app/AkGaming.Website/dist /usr/share/caddy
COPY AkGaming.Website/Caddyfile /etc/caddy/Caddyfile

EXPOSE 80
