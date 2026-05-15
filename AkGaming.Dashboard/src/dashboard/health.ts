import type { StatusState } from '../types'

async function fetchWithTimeout(url: string, timeoutMs: number) {
  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => {
    controller.abort()
  }, timeoutMs)

  try {
    await fetch(url, {
      method: 'GET',
      mode: 'no-cors',
      cache: 'no-store',
      signal: controller.signal,
    })
    return true
  } catch {
    return false
  } finally {
    window.clearTimeout(timeoutId)
  }
}

export async function probeShortcut(url: string): Promise<StatusState> {
  const isReachable = await fetchWithTimeout(url, 2500)
  if (isReachable) {
    return 'online'
  }

  try {
    const hostname = new URL(url).hostname.toLowerCase()
    const faviconReachable = await fetchWithTimeout(`https://icons.duckduckgo.com/ip3/${hostname}.ico`, 2000)
    return faviconReachable ? 'online' : 'offline'
  } catch {
    return 'offline'
  }
}
