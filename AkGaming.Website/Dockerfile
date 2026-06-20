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

CMD ["node", "server.js"]
