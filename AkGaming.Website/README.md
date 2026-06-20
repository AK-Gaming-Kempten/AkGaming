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

## Current content bridge

Existing posts continue to be authored as MD/MDX files in `src/data/posts`. Game, team,
highlight, and gallery data is exposed through internal content routes under `/api/content/*`.
This is a transitional boundary: the next CMS step can move these reads to a mounted content
store without changing the public page routes.
