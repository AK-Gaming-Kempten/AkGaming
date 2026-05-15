import { SHORTCUTS } from '../data/sites'

export function buildLogoUrl(url: string) {
  try {
    const hostname = new URL(url).hostname.toLowerCase()
    return `https://icons.duckduckgo.com/ip3/${hostname}.ico`
  } catch {
    return ''
  }
}

export function shortcutInitials(title: string) {
  return title
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

export function getShortcutById(siteId: string) {
  return SHORTCUTS.find((shortcut) => shortcut.id === siteId) ?? null
}
