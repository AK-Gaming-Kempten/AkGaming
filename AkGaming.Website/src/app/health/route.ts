export const dynamic = 'force-dynamic'

export function GET() {
  return new Response('Healthy', {
    status: 200,
    headers: {
      'Cache-Control': 'no-store',
      'Content-Type': 'text/plain; charset=utf-8',
    },
  })
}

export function HEAD() {
  return new Response(null, {
    status: 200,
    headers: {
      'Cache-Control': 'no-store',
    },
  })
}
