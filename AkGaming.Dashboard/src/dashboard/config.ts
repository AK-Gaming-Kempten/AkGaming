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
  if (preset === 'custom') {
    return {
      categories: [],
      siteCategory: {},
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
    unusedSiteIds,
    categoryLayout,
  }
}
