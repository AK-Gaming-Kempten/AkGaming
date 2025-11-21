# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Serve with Caddy (static)
FROM caddy:2-alpine

COPY --from=build /app/dist /usr/share/caddy