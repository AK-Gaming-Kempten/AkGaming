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
ENV AKG_WEBSITE_CONTENT_ROOT=/var/lib/akgaming-website/content
ENV AKG_WEBSITE_MEDIA_ROOT=/var/lib/akgaming-website/media

COPY --from=build /app/AkGaming.Website/.next/standalone ./
COPY --from=build /app/AkGaming.Website/.next/static ./.next/static
COPY --from=build /app/AkGaming.Website/public ./public
COPY --from=build /app/AkGaming.Website/scripts/healthcheck.cjs ./healthcheck.cjs
COPY --from=build /app/AkGaming.Website/scripts/initialize-storage.cjs ./initialize-storage.cjs
COPY --from=build /app/AkGaming.Website/scripts/docker-entrypoint.sh ./docker-entrypoint.sh
COPY --from=build /app/AkGaming.Website/src/data ./seed/content
# Serve media through the runtime route so uploads use the mounted storage.
RUN mv ./public/media ./seed/media

VOLUME ["/var/lib/akgaming-website"]
ENTRYPOINT ["/bin/sh", "/app/docker-entrypoint.sh"]

EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD ["node", "healthcheck.cjs"]

CMD ["node", "server.js"]
