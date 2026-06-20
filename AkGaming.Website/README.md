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
