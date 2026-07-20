# Build stage (monorepo context expected)
FROM node:22-alpine AS build
WORKDIR /app/AkGaming.Website

COPY AkGaming.Website/package*.json ./
RUN npm ci

COPY AkGaming.Website ./
COPY AkGaming.Core ../AkGaming.Core

RUN npm run build

# Run the server-rendered website
FROM node:22-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

COPY --from=build /app/AkGaming.Website/.next/standalone ./
COPY --from=build /app/AkGaming.Website/.next/static ./.next/static
COPY --from=build /app/AkGaming.Website/public ./public

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD node -e "fetch('http://127.0.0.1:3000/health').then(response => process.exit(response.ok ? 0 : 1)).catch(() => process.exit(1))"

CMD ["node", "server.js"]
