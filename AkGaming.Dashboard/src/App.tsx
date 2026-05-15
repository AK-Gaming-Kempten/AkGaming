import { useEffect, useEffectEvent, useMemo, useState, type CSSProperties } from 'react'
import { CATEGORY_ORDER, PRESET_LABELS, PRESET_ORDER, SHORTCUTS, isVisibleInPreset } from './data/sites'
import { useLocalStorageState } from './hooks/useLocalStorageState'
import type { PresetId, ShortcutStatus, SiteShortcut, StatusState } from './types'

const STORAGE_PREFIX = 'akg-dashboard'
const CONFIG_STORAGE_KEY = `${STORAGE_PREFIX}:dashboard-config`

interface DashboardConfig {
  categories: string[]
  siteCategory: Record<string, string>
  unusedSiteIds: string[]
  hiddenCategories?: string[]
  categoryLayout?: Record<string, CategoryLayout>
}

interface CategoryLayout {
  colSpan: number
  rowSpan: number
}

const DEFAULT_CATEGORIES = (() => {
  const categories = new Set<string>(CATEGORY_ORDER)
  for (const shortcut of SHORTCUTS) {
    categories.add(shortcut.category)
  }
  return [...categories]
})()

function buildConfigFromPreset(preset: PresetId): DashboardConfig {
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

function buildLogoUrl(url: string) {
  try {
    const hostname = new URL(url).hostname.toLowerCase()
    return `https://icons.duckduckgo.com/ip3/${hostname}.ico`
  } catch {
    return ''
  }
}

function shortcutInitials(title: string) {
  return title
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

function getShortcutById(siteId: string) {
  return SHORTCUTS.find((shortcut) => shortcut.id === siteId) ?? null
}

function normalizeConfig(config: DashboardConfig): DashboardConfig {
  const knownIds = new Set(SHORTCUTS.map((shortcut) => shortcut.id))
  const categories = config.categories.length > 0 ? [...config.categories] : [...DEFAULT_CATEGORIES]
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

function normalizeSpan(value: number | undefined, fallback: number) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    return fallback
  }

  return Math.min(3, Math.max(1, Math.round(value)))
}

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

async function probeShortcut(url: string): Promise<StatusState> {
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

function App() {
  const [config, setConfig] = useLocalStorageState<DashboardConfig | null>(CONFIG_STORAGE_KEY, null)
  const [newCategoryName, setNewCategoryName] = useState('')
  const [activeDialog, setActiveDialog] = useState<'preset' | 'category' | 'entry' | null>(null)
  const [entryTargetCategory, setEntryTargetCategory] = useState('')
  const [isEditMode, setIsEditMode] = useState(false)
  const [draggingSiteId, setDraggingSiteId] = useState<string | null>(null)
  const [failedLogos, setFailedLogos] = useState<Record<string, boolean>>({})
  const [statuses, setStatuses] = useState<Record<string, ShortcutStatus>>(() => {
    const initialStatuses: Record<string, ShortcutStatus> = {}
    for (const shortcut of SHORTCUTS) {
      initialStatuses[shortcut.id] = {
        state: 'unknown',
        checkedAt: null,
      }
    }
    return initialStatuses
  })

  const dashboardConfig = useMemo(() => (config ? normalizeConfig(config) : null), [config])
  const activeCategories = dashboardConfig?.categories ?? []
  const activeSiteIds = dashboardConfig ? Object.keys(dashboardConfig.siteCategory) : []
  const activeShortcuts = activeSiteIds
    .map((siteId) => getShortcutById(siteId))
    .filter((shortcut): shortcut is SiteShortcut => shortcut !== null)
  const unusedShortcuts = (dashboardConfig?.unusedSiteIds ?? [])
    .map((siteId) => getShortcutById(siteId))
    .filter((shortcut): shortcut is SiteShortcut => shortcut !== null)

  const shortcutsByCategory = useMemo(() => {
    const grouped: Record<string, SiteShortcut[]> = {}
    if (!dashboardConfig) {
      return grouped
    }

    for (const shortcut of activeShortcuts) {
      const category = dashboardConfig.siteCategory[shortcut.id]
      grouped[category] ??= []
      grouped[category].push(shortcut)
    }

    return grouped
  }, [activeShortcuts, dashboardConfig])

  useEffect(() => {
    if (isEditMode) {
      return
    }

    setActiveDialog(null)
    setEntryTargetCategory('')
    setDraggingSiteId(null)
  }, [isEditMode])

  const runHealthChecks = useEffectEvent(async () => {
    setStatuses((currentStatuses) => {
      const nextStatuses = { ...currentStatuses }
      for (const shortcut of activeShortcuts) {
        const previous = currentStatuses[shortcut.id]
        nextStatuses[shortcut.id] = {
          state: 'checking',
          checkedAt: previous?.checkedAt ?? null,
        }
      }
      return nextStatuses
    })

    const checks: Array<{ id: string; state: StatusState }> = []
    for (const shortcut of activeShortcuts) {
      const state = await probeShortcut(shortcut.url)
      checks.push({ id: shortcut.id, state })
    }

    setStatuses((currentStatuses) => {
      const nextStatuses = { ...currentStatuses }
      for (const check of checks) {
        nextStatuses[check.id] = {
          state: check.state,
          checkedAt: Date.now(),
        }
      }
      return nextStatuses
    })
  })

  function loadPreset(preset: PresetId) {
    setConfig(buildConfigFromPreset(preset))
    setNewCategoryName('')
    setActiveDialog(null)
    setEntryTargetCategory('')
  }

  function updateConfig(updater: (currentConfig: DashboardConfig) => DashboardConfig) {
    setConfig((currentConfig) => {
      const baseConfig = currentConfig ? normalizeConfig(currentConfig) : buildConfigFromPreset('all')
      return normalizeConfig(updater(baseConfig))
    })
  }

  function addCategory() {
    const trimmed = newCategoryName.trim()
    if (!trimmed) {
      return
    }

    updateConfig((currentConfig) => {
      if (currentConfig.categories.includes(trimmed)) {
        return currentConfig
      }

      return {
        ...currentConfig,
        categories: [...currentConfig.categories, trimmed],
      }
    })
    setNewCategoryName('')
    setActiveDialog(null)
  }

  function moveSiteToCategory(siteId: string, category: string) {
    updateConfig((currentConfig) => ({
      ...currentConfig,
      siteCategory: {
        ...currentConfig.siteCategory,
        [siteId]: category,
      },
      unusedSiteIds: currentConfig.unusedSiteIds.filter((unusedSiteId) => unusedSiteId !== siteId),
    }))
  }

  function removeSiteFromBoard(siteId: string) {
    updateConfig((currentConfig) => {
      const siteCategory = { ...currentConfig.siteCategory }
      delete siteCategory[siteId]
      const unusedSiteIds = currentConfig.unusedSiteIds.includes(siteId)
        ? currentConfig.unusedSiteIds
        : [...currentConfig.unusedSiteIds, siteId]

      return {
        ...currentConfig,
        siteCategory,
        unusedSiteIds,
      }
    })
  }

  function moveCategory(category: string, direction: -1 | 1) {
    updateConfig((currentConfig) => {
      const currentIndex = currentConfig.categories.indexOf(category)
      const nextIndex = currentIndex + direction
      if (currentIndex < 0 || nextIndex < 0 || nextIndex >= currentConfig.categories.length) {
        return currentConfig
      }

      const categories = [...currentConfig.categories]
      const [movedCategory] = categories.splice(currentIndex, 1)
      categories.splice(nextIndex, 0, movedCategory)

      return {
        ...currentConfig,
        categories,
      }
    })
  }

  function moveCategoryToIndex(category: string, nextIndex: number) {
    updateConfig((currentConfig) => {
      const currentIndex = currentConfig.categories.indexOf(category)
      if (currentIndex < 0 || nextIndex < 0 || nextIndex >= currentConfig.categories.length || currentIndex === nextIndex) {
        return currentConfig
      }

      const categories = [...currentConfig.categories]
      const [movedCategory] = categories.splice(currentIndex, 1)
      categories.splice(nextIndex, 0, movedCategory)

      return {
        ...currentConfig,
        categories,
      }
    })
  }

  function resizeCategory(category: string, dimension: keyof CategoryLayout, delta: -1 | 1) {
    updateConfig((currentConfig) => {
      const currentLayout = currentConfig.categoryLayout?.[category] ?? { colSpan: 1, rowSpan: 1 }
      const nextLayout = {
        ...currentLayout,
        [dimension]: normalizeSpan(currentLayout[dimension] + delta, 1),
      }

      return {
        ...currentConfig,
        categoryLayout: {
          ...currentConfig.categoryLayout,
          [category]: nextLayout,
        },
      }
    })
  }

  function deleteCategory(category: string) {
    updateConfig((currentConfig) => {
      const siteCategory = { ...currentConfig.siteCategory }
      const unusedSiteIds = [...currentConfig.unusedSiteIds]
      const unusedSet = new Set(unusedSiteIds)

      for (const [siteId, assignedCategory] of Object.entries(currentConfig.siteCategory)) {
        if (assignedCategory !== category) {
          continue
        }

        delete siteCategory[siteId]
        if (!unusedSet.has(siteId)) {
          unusedSiteIds.push(siteId)
          unusedSet.add(siteId)
        }
      }

      const categoryLayout = { ...currentConfig.categoryLayout }
      delete categoryLayout[category]

      return {
        ...currentConfig,
        categories: currentConfig.categories.filter((currentCategory) => currentCategory !== category),
        siteCategory,
        unusedSiteIds,
        categoryLayout,
      }
    })
  }

  function openEntryDialog(category: string) {
    setEntryTargetCategory(category)
    setActiveDialog('entry')
  }

  function closeDialog() {
    setActiveDialog(null)
    setEntryTargetCategory('')
  }

  if (!dashboardConfig) {
    return (
      <main className="dashboard setup-dashboard">
        <section className="setup-dialog" role="dialog" aria-labelledby="setup-title" aria-modal="true">
          <h1 id="setup-title">Choose a starter preset</h1>
          <p>Select the collection to start from. You can customize it afterwards or load another preset later.</p>
          <div className="setup-preset-grid">
            {PRESET_ORDER.map((preset) => (
              <button key={preset} type="button" onClick={() => loadPreset(preset)}>
                {PRESET_LABELS[preset]}
              </button>
            ))}
          </div>
        </section>
      </main>
    )
  }

  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <div>
          <h1>AK Gaming Dashboard</h1>
          <p>Shortcut board for internal services and public channels.</p>
        </div>
        <div className="dashboard-header-controls">
          <div className="dashboard-actions">
            <button type="button" className="header-icon-btn" title="Refresh status" aria-label="Refresh status" onClick={() => void runHealthChecks()}>
              <span className="bi bi-arrow-clockwise" aria-hidden="true"></span>
            </button>
            <button
              type="button"
              className={isEditMode ? 'header-icon-btn edit-toggle active' : 'header-icon-btn edit-toggle'}
              title={isEditMode ? 'Disable edit mode' : 'Enable edit mode'}
              aria-label={isEditMode ? 'Disable edit mode' : 'Enable edit mode'}
              onClick={() => setIsEditMode((currentIsEditMode) => !currentIsEditMode)}
            >
              <span className={isEditMode ? 'bi bi-pencil-fill' : 'bi bi-pencil'} aria-hidden="true"></span>
            </button>
            {isEditMode && (
              <button type="button" className="header-icon-btn" title="Load preset" aria-label="Load preset" onClick={() => setActiveDialog('preset')}>
                <span className="bi bi-layout-three-columns" aria-hidden="true"></span>
              </button>
            )}
          </div>
        </div>
      </header>

      <section className="board-grid">
        {activeCategories.map((category) => {
          const shortcuts = shortcutsByCategory[category] ?? []
          const categoryIndex = dashboardConfig.categories.indexOf(category)
          const categoryLayout = dashboardConfig.categoryLayout?.[category] ?? { colSpan: 1, rowSpan: 1 }
          const occupiedEntryRows = Math.max(1, Math.ceil(shortcuts.length / categoryLayout.colSpan))
          const requiredEntryBlocks = Math.max(categoryLayout.rowSpan, Math.ceil(occupiedEntryRows / 3))
          const visibleEntryRows = requiredEntryBlocks * 3
          const categoryStyle = {
            '--category-col-span': categoryLayout.colSpan,
            '--category-row-span': categoryLayout.rowSpan,
            '--entry-columns': categoryLayout.colSpan,
            '--entry-visible-rows': visibleEntryRows,
            '--entry-visible-gaps': Math.max(0, visibleEntryRows - 1),
            '--category-header-height': isEditMode ? '6.95rem' : '3.45rem',
            '--category-footer-height': isEditMode ? '2.97rem' : '0rem',
          } as CSSProperties

          return (
            <article
              key={category}
              className={draggingSiteId ? 'category-column drag-target' : 'category-column'}
              style={categoryStyle}
              onDragOver={(event) => {
                if (!isEditMode) {
                  return
                }

                event.preventDefault()
              }}
              onDrop={(event) => {
                if (!isEditMode) {
                  return
                }

                event.preventDefault()
                const dataTransferSiteId = event.dataTransfer.getData('text/plain')
                const siteId = dataTransferSiteId || draggingSiteId
                if (siteId) {
                  moveSiteToCategory(siteId, category)
                }
                setDraggingSiteId(null)
              }}
            >
              <header className="category-column-header">
                <h2>{category}</h2>
                <div className="category-column-actions">
                  <span className="category-count">{shortcuts.length}</span>
                  {isEditMode && (
                    <div className="category-control-grid" aria-label={`${category} layout controls`}>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Move earlier"
                        aria-label="Move earlier"
                        disabled={categoryIndex <= 0}
                        onClick={() => moveCategory(category, -1)}
                      >
                        <span className="bi bi-caret-left-fill" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Shorter"
                        aria-label="Shorter"
                        disabled={categoryLayout.rowSpan <= 1}
                        onClick={() => resizeCategory(category, 'rowSpan', -1)}
                      >
                        <span className="bi bi-dash-lg" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Move later"
                        aria-label="Move later"
                        disabled={categoryIndex >= dashboardConfig.categories.length - 1}
                        onClick={() => moveCategory(category, 1)}
                      >
                        <span className="bi bi-caret-right-fill" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Narrower"
                        aria-label="Narrower"
                        disabled={categoryLayout.colSpan <= 1}
                        onClick={() => resizeCategory(category, 'colSpan', -1)}
                      >
                        <span className="bi bi-dash-lg" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Delete category"
                        aria-label="Delete category"
                        onClick={() => deleteCategory(category)}
                      >
                        <span className="bi bi-x-lg" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Wider"
                        aria-label="Wider"
                        disabled={categoryLayout.colSpan >= 3}
                        onClick={() => resizeCategory(category, 'colSpan', 1)}
                      >
                        <span className="bi bi-plus-lg" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Move first"
                        aria-label="Move first"
                        disabled={categoryIndex <= 0}
                        onClick={() => moveCategoryToIndex(category, 0)}
                      >
                        <span className="bi bi-caret-up-fill" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Taller"
                        aria-label="Taller"
                        disabled={categoryLayout.rowSpan >= 3}
                        onClick={() => resizeCategory(category, 'rowSpan', 1)}
                      >
                        <span className="bi bi-plus-lg" aria-hidden="true"></span>
                      </button>
                      <button
                        type="button"
                        className="category-tool-btn"
                        title="Move last"
                        aria-label="Move last"
                        disabled={categoryIndex >= dashboardConfig.categories.length - 1}
                        onClick={() => moveCategoryToIndex(category, dashboardConfig.categories.length - 1)}
                      >
                        <span className="bi bi-caret-down-fill" aria-hidden="true"></span>
                      </button>
                    </div>
                  )}
                </div>
              </header>
              <div className="shortcut-list">
                {shortcuts.map((shortcut) => {
                  const status = statuses[shortcut.id]?.state ?? 'checking'
                  const colorStyle = { '--blob-color': shortcut.color } as CSSProperties
                  const hasFailedLogo = failedLogos[shortcut.id] === true

                  return (
                    <div
                      key={shortcut.id}
                      className="shortcut-item"
                      style={colorStyle}
                      draggable={isEditMode}
                      onDragStart={(event) => {
                        if (!isEditMode) {
                          return
                        }

                        event.dataTransfer.setData('text/plain', shortcut.id)
                        event.dataTransfer.effectAllowed = 'move'
                        setDraggingSiteId(shortcut.id)
                      }}
                      onDragEnd={() => {
                        setDraggingSiteId(null)
                      }}
                    >
                      <a href={shortcut.url} target="_blank" rel="noopener noreferrer" className="shortcut-link">
                        <span
                          className={
                            status === 'online'
                              ? 'status-dot online'
                              : status === 'offline'
                                ? 'status-dot offline'
                                : status === 'checking'
                                  ? 'status-dot checking'
                                  : 'status-dot unknown'
                          }
                        />
                        {hasFailedLogo ? (
                          <span className="logo-fallback">{shortcutInitials(shortcut.title)}</span>
                        ) : (
                          <img
                            src={buildLogoUrl(shortcut.url)}
                            alt=""
                            className="logo"
                            loading="lazy"
                            onError={() => {
                              setFailedLogos((currentFailedLogos) => ({
                                ...currentFailedLogos,
                                [shortcut.id]: true,
                              }))
                            }}
                          />
                        )}
                        <span className="shortcut-title">{shortcut.title}</span>
                      </a>
                      {isEditMode && (
                        <button type="button" className="shortcut-remove" onClick={() => removeSiteFromBoard(shortcut.id)}>
                          Remove
                        </button>
                      )}
                    </div>
                  )
                })}
                {shortcuts.length === 0 && <div className="empty-category">No shortcuts</div>}
              </div>
              {isEditMode && (
                <button type="button" className="category-add-entry" onClick={() => openEntryDialog(category)}>
                  +
                </button>
              )}
            </article>
          )
        })}
        {isEditMode && (
          <button type="button" className="category-column ghost-category" onClick={() => setActiveDialog('category')}>
            <span>+</span>
          </button>
        )}
      </section>

      {isEditMode && activeDialog !== null && (
        <div className="dialog-backdrop" role="presentation" onMouseDown={closeDialog}>
          <section className="action-dialog" role="dialog" aria-modal="true" onMouseDown={(event) => event.stopPropagation()}>
            {activeDialog === 'preset' && (
              <>
                <header className="dialog-header">
                  <h2>Load preset</h2>
                  <button type="button" onClick={closeDialog}>
                    Close
                  </button>
                </header>
                <div className="setup-preset-grid">
                  {PRESET_ORDER.map((preset) => (
                    <button key={preset} type="button" onClick={() => loadPreset(preset)}>
                      {PRESET_LABELS[preset]}
                    </button>
                  ))}
                </div>
              </>
            )}

            {activeDialog === 'category' && (
              <>
                <header className="dialog-header">
                  <h2>Add category</h2>
                  <button type="button" onClick={closeDialog}>
                    Close
                  </button>
                </header>
                <div className="dialog-form">
                  <input
                    type="text"
                    value={newCategoryName}
                    onChange={(event) => setNewCategoryName(event.currentTarget.value)}
                    placeholder="New category name"
                    autoFocus
                    onKeyDown={(event) => {
                      if (event.key === 'Enter') {
                        event.preventDefault()
                        addCategory()
                      }
                    }}
                  />
                  <button type="button" onClick={addCategory}>
                    Add
                  </button>
                </div>
              </>
            )}

            {activeDialog === 'entry' && (
              <>
                <header className="dialog-header">
                  <h2>Add entry</h2>
                  <button type="button" onClick={closeDialog}>
                    Close
                  </button>
                </header>
                <div className="unused-entry-grid">
                  {unusedShortcuts.map((shortcut) => {
                    const colorStyle = { '--blob-color': shortcut.color } as CSSProperties
                    const hasFailedLogo = failedLogos[shortcut.id] === true

                    return (
                      <button
                        key={shortcut.id}
                        type="button"
                        className="shortcut-item unused-entry-option"
                        style={colorStyle}
                        onClick={() => {
                          moveSiteToCategory(shortcut.id, entryTargetCategory)
                          closeDialog()
                        }}
                      >
                        <span className="shortcut-link">
                          {hasFailedLogo ? (
                            <span className="logo-fallback">{shortcutInitials(shortcut.title)}</span>
                          ) : (
                            <img
                              src={buildLogoUrl(shortcut.url)}
                              alt=""
                              className="logo"
                              loading="lazy"
                              onError={() => {
                                setFailedLogos((currentFailedLogos) => ({
                                  ...currentFailedLogos,
                                  [shortcut.id]: true,
                                }))
                              }}
                            />
                          )}
                          <span className="shortcut-title">{shortcut.title}</span>
                        </span>
                      </button>
                    )
                  })}
                  {unusedShortcuts.length === 0 && <div className="empty-category">No unused entries</div>}
                </div>
              </>
            )}
          </section>
        </div>
      )}
    </main>
  )
}

export default App
