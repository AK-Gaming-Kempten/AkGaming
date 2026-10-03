# AK Gaming Website

The public AK Gaming website is a server-rendered Next.js application. It retains the existing
React components and MDX content while providing an application server for the planned CMS,
draft previews, and runtime content loading.

## Development

Requires Node.js 20 or newer.

```bash
npm ci
npm run dev
```

## Validation and production build

```bash
npm run lint
npm run build
npm run start
```

The build produces a standalone Next.js server. It listens on port `3000` by default; use the
`PORT` environment variable to change it.

## CMS authentication

The CMS is available at `/cms` and is intentionally separate from the public website shell. It
requires an administrator account from `identity.akgaming.de`.

For local development, copy `.env.example` to `.env.local` and set `AUTH_SECRET` to a random
value. The development Identity configuration registers `akgaming-website-cms` with the callback
URL `http://localhost:3000/api/auth/callback/akgaming`. When the issuer is a local HTTPS endpoint,
the CMS trusts its self-signed development certificate only in development mode.

Production must register the same client in Identity with the real CMS callback and post-logout
URLs, and provide the corresponding environment variables to the website container.

## Current content bridge

Existing posts continue to be authored as MD/MDX files in `src/data/posts`. Game, team,
highlight, and gallery data is exposed through internal content routes under `/api/content/*`.
This is a transitional boundary: the next CMS step can move these reads to a mounted content
store without changing the public page routes.

## Deployment healthcheck

The Docker image probes `http://127.0.0.1:${PORT}/health` (port 3000 by default).
It expects HTTP 200 and the body `Healthy`; failures include the URL and status or
connection error in Docker's healthcheck output. Coolify's separate HTTP probe
should use `/health` on the application's configured port.

If the container reports healthy but the domain returns 404, compare the deployed
commit/image and reverse-proxy target. A failed rollout may leave the previous
container serving traffic. Inspect the new container's startup logs and healthcheck
output before changing the endpoint.

## Persistent CMS storage (required in production)

In Coolify, add a persistent **named volume or host directory** mounted at
`/var/lib/akgaming-website`. Reuse the same volume across deployments; production
and test must use separate volumes. The Dockerfile's `VOLUME` declaration alone
does not guarantee that Coolify reattaches the same storage on replacement.

The container sets `AKG_WEBSITE_CONTENT_ROOT` to the `content` subdirectory and
`AKG_WEBSITE_MEDIA_ROOT` to `media`. Together these hold posts, drafts, folders,
highlights, esports catalogs/teams, and uploads. Back up the entire mount.

Startup seeds each directory from bundled content only when it is completely
empty. Existing directories are kept intact; deployments never sync repository
content over CMS files. New repository content/media is therefore not automatically
merged into an existing CMS store: import it deliberately through the CMS.

Before the first deployment with persistent storage, export the previous
container's `/app/src/data` and `/app/public/media` (or its configured roots).
Restore them into the volume's `content` and `media` directories before startup.
Keep a separate backup of those exports. If the previous container has already
been removed, recovery requires a backup or an existing host mount/volume; its
image contains the bundled defaults, not subsequent CMS edits.

Do not redeploy again until any retained old container has been copied. Inspect
Coolify's previous deployment containers and Docker mounts; `docker cp` works
with stopped containers too.
