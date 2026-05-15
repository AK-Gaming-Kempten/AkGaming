import { CATEGORY_ORDER, SHORTCUTS, isVisibleInPreset } from '../data/sites'
import type { PresetId } from '../types'
import type { CategoryLayout, DashboardConfig } from './types'

export const STORAGE_PREFIX = 'akg-dashboard'
export const CONFIG_STORAGE_KEY = `${STORAGE_PREFIX}:dashboard-config`

export const PRESET_STYLE: Record<PresetId, { color: string; icon: string }> = {
  vorstand: { color: '#f1c40f', icon: 'bi-award-fill' },
  dev: { color: '#7e3ff2', icon: 'bi-code-slash' },
  eventleitung: { color: '#e91e63', icon: 'bi-calendar-event-fill' },
  all: { color: '#2ecc71', icon: 'bi-collection-fill' },
  custom: { color: '#8f9aa3', icon: 'bi-sliders' },
}

export function normalizeSpan(value: number | undefined, fallback: number) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    return fallback
  }

  return Math.min(3, Math.max(1, Math.round(value)))
}

export function buildConfigFromPreset(preset: PresetId): DashboardConfig {
  if (preset === 'vorstand') {
    const siteCategory: Record<string, string> = {
      website: 'Public',
      twitch: 'Public',
      discord: 'Public',
      youtube: 'Public',
      instagram: 'Public',
      linkedin: 'Public',
      facebook: 'Public',
      itch: 'Public',
      tournaments: 'Public',
      management: 'Management',
      drive: 'Management',
      kanban: 'Management',
      identity: 'Management',
      bitwarden: 'Security',
      vaultwarden: 'Security',
    }

    const siteOrder = [
      'website',
      'twitch',
      'discord',
      'youtube',
      'instagram',
      'linkedin',
      'facebook',
      'itch',
      'tournaments',
      'management',
      'drive',
      'kanban',
      'identity',
      'bitwarden',
      'vaultwarden',
    ]

    const configuredIds = new Set(siteOrder)
    const unusedSiteIds = SHORTCUTS.filter((shortcut) => !configuredIds.has(shortcut.id)).map((shortcut) => shortcut.id)

    return {
      categories: ['Public', 'Management', 'Security'],
      siteCategory,
      siteOrder,
      unusedSiteIds,
      categoryLayout: {
        Public: { colSpan: 2, rowSpan: 1 },
        Management: { colSpan: 1, rowSpan: 1 },
        Security: { colSpan: 1, rowSpan: 1 },
      },
    }
  }

  if (preset === 'dev') {
    const siteCategory: Record<string, string> = {
      'identity-test': 'Test Systems',
      'website-test': 'Test Systems',
      'management-test': 'Test Systems',
      'tournaments-test': 'Test Systems',
      identity: 'Live Systems',
      website: 'Live Systems',
      management: 'Live Systems',
      tournaments: 'Live Systems',
      kanban: 'Management',
      github: 'Infrastructure',
      hetzner: 'Infrastructure',
      coolify: 'Infrastructure',
      vaultwarden: 'Security',
    }

    const siteOrder = [
      'identity-test',
      'website-test',
      'management-test',
      'tournaments-test',
      'identity',
      'website',
      'management',
      'tournaments',
      'kanban',
      'github',
      'hetzner',
      'coolify',
      'vaultwarden',
    ]

    const configuredIds = new Set(siteOrder)
    const unusedSiteIds = SHORTCUTS.filter((shortcut) => !configuredIds.has(shortcut.id)).map((shortcut) => shortcut.id)

    return {
      categories: ['Test Systems', 'Management', 'Infrastructure', 'Security', 'Live Systems'],
      siteCategory,
      siteOrder,
      unusedSiteIds,
      categoryLayout: {
        'Test Systems': { colSpan: 2, rowSpan: 1 },
        Management: { colSpan: 1, rowSpan: 1 },
        Infrastructure: { colSpan: 1, rowSpan: 1 },
        Security: { colSpan: 1, rowSpan: 1 },
        'Live Systems': { colSpan: 2, rowSpan: 1 },
      },
    }
  }

  if (preset === 'eventleitung') {
    const siteCategory: Record<string, string> = {
      tournaments: 'Tools',
      management: 'Tools',
      kanban: 'Management',
      drive: 'Management',
      vaultwarden: 'Management',
      website: 'Socials',
      discord: 'Socials',
      twitch: 'Socials',
      instagram: 'Socials',
      youtube: 'Socials',
      itch: 'Socials',
      facebook: 'Socials',
      linkedin: 'Socials',
    }

    const siteOrder = [
      'tournaments',
      'management',
      'kanban',
      'drive',
      'vaultwarden',
      'website',
      'discord',
      'twitch',
      'instagram',
      'youtube',
      'itch',
      'facebook',
      'linkedin',
    ]

    const configuredIds = new Set(siteOrder)
    const unusedSiteIds = SHORTCUTS.filter((shortcut) => !configuredIds.has(shortcut.id)).map((shortcut) => shortcut.id)

    return {
      categories: ['Tools', 'Management', 'Socials'],
      siteCategory,
      siteOrder,
      unusedSiteIds,
      categoryLayout: {
        Tools: { colSpan: 1, rowSpan: 1 },
        Management: { colSpan: 1, rowSpan: 1 },
        Socials: { colSpan: 2, rowSpan: 1 },
      },
    }
  }

  if (preset === 'all') {
    const siteCategory: Record<string, string> = {
      website: 'Public',
      discord: 'Public',
      instagram: 'Public',
      youtube: 'Public',
      linkedin: 'Public',
      facebook: 'Public',
      itch: 'Public',
      twitch: 'Public',
      kanban: 'Management',
      management: 'Management',
      drive: 'Management',
      tournaments: 'Management',
      'identity-test': 'Test',
      'management-test': 'Test',
      'website-test': 'Test',
      'tournaments-test': 'Test',
      hetzner: 'Infrastructure',
      coolify: 'Infrastructure',
      github: 'Infrastructure',
      identity: 'Security',
      bitwarden: 'Security',
      vaultwarden: 'Security',
    }

    const siteOrder = [
      'website',
      'discord',
      'instagram',
      'youtube',
      'linkedin',
      'facebook',
      'itch',
      'twitch',
      'kanban',
      'management',
      'drive',
      'tournaments',
      'identity-test',
      'management-test',
      'website-test',
      'tournaments-test',
      'hetzner',
      'coolify',
      'github',
      'identity',
      'bitwarden',
      'vaultwarden',
    ]

    const configuredIds = new Set(siteOrder)
    const unusedSiteIds = SHORTCUTS.filter((shortcut) => !configuredIds.has(shortcut.id)).map((shortcut) => shortcut.id)

    return {
      categories: ['Public', 'Management', 'Test', 'Infrastructure', 'Security'],
      siteCategory,
      siteOrder,
      unusedSiteIds,
      categoryLayout: {
        Public: { colSpan: 2, rowSpan: 1 },
        Management: { colSpan: 1, rowSpan: 1 },
        Test: { colSpan: 1, rowSpan: 1 },
        Infrastructure: { colSpan: 1, rowSpan: 1 },
        Security: { colSpan: 1, rowSpan: 1 },
      },
    }
  }

  if (preset === 'custom') {
    return {
      categories: [],
      siteCategory: {},
      siteOrder: [],
      unusedSiteIds: SHORTCUTS.map((shortcut) => shortcut.id),
      categoryLayout: {},
    }
  }

  const includedShortcuts = SHORTCUTS.filter((shortcut) => isVisibleInPreset(shortcut, preset))
  const includedIds = new Set(includedShortcuts.map((shortcut) => shortcut.id))
  const categories = new Set<string>()
  const siteCategory: Record<string, string> = {}

  for (const category of CATEGORY_ORDER) {
    if (includedShortcuts.some((shortcut) => shortcut.category === category)) {
      categories.add(category)
    }
  }

  for (const shortcut of includedShortcuts) {
    categories.add(shortcut.category)
    siteCategory[shortcut.id] = shortcut.category
  }

  return {
    categories: [...categories],
    siteCategory,
    siteOrder: includedShortcuts.map((shortcut) => shortcut.id),
    unusedSiteIds: SHORTCUTS.filter((shortcut) => !includedIds.has(shortcut.id)).map((shortcut) => shortcut.id),
    categoryLayout: {},
  }
}

export function normalizeConfig(config: DashboardConfig): DashboardConfig {
  const knownIds = new Set(SHORTCUTS.map((shortcut) => shortcut.id))
  const categories = [...config.categories]
  const categorySet = new Set(categories)
  const siteCategory: Record<string, string> = {}
  const categoryLayout: Record<string, CategoryLayout> = {}

  for (const [siteId, category] of Object.entries(config.siteCategory)) {
    if (!knownIds.has(siteId)) {
      continue
    }

    siteCategory[siteId] = category
    if (!categorySet.has(category)) {
      categories.push(category)
      categorySet.add(category)
    }
  }

  const configuredIds = new Set(Object.keys(siteCategory))
  const siteOrder = (config.siteOrder ?? [])
    .filter((siteId) => configuredIds.has(siteId))
    .filter((siteId, index, ids) => ids.indexOf(siteId) === index)

  for (const siteId of Object.keys(siteCategory)) {
    if (!siteOrder.includes(siteId)) {
      siteOrder.push(siteId)
    }
  }

  const unusedSiteIds = config.unusedSiteIds.filter((siteId) => knownIds.has(siteId) && !configuredIds.has(siteId))
  const unusedSet = new Set(unusedSiteIds)

  for (const shortcut of SHORTCUTS) {
    if (!configuredIds.has(shortcut.id) && !unusedSet.has(shortcut.id)) {
      unusedSiteIds.push(shortcut.id)
      unusedSet.add(shortcut.id)
    }
  }

  for (const category of categories) {
    const layout = config.categoryLayout?.[category]
    categoryLayout[category] = {
      colSpan: normalizeSpan(layout?.colSpan, 1),
      rowSpan: normalizeSpan(layout?.rowSpan, 1),
    }
  }

  return {
    categories,
    siteCategory,
    siteOrder,
    unusedSiteIds,
    categoryLayout,
  }
}
