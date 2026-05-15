import { useEffect, useEffectEvent, useMemo, useState, type CSSProperties } from 'react'
import { PRESET_ORDER, SHORTCUTS } from './data/sites'
import { useLocalStorageState } from './hooks/useLocalStorageState'
import type { PresetId, ShortcutStatus, SiteShortcut, StatusState } from './types'
import akGamingLogo from '../../AkGaming.Core/Theme/wwwroot/images/icons/AKG_Logos/Default.png'
import { CategoryControlGrid } from './components/CategoryControlGrid'
import { PresetCard } from './components/PresetCard'
import { ShortcutTile } from './components/ShortcutTile'
import { probeShortcut } from './dashboard/health'
import { CONFIG_STORAGE_KEY, buildConfigFromPreset, normalizeConfig, normalizeSpan } from './dashboard/config'
import { getShortcutById } from './dashboard/shortcuts'
import type { ActiveDialog, CategoryLayout, DashboardConfig } from './dashboard/types'

function App() {
  const [config, setConfig] = useLocalStorageState<DashboardConfig | null>(CONFIG_STORAGE_KEY, null)
  const [newCategoryName, setNewCategoryName] = useState('')
  const [activeDialog, setActiveDialog] = useState<ActiveDialog>(null)
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
              <PresetCard key={preset} preset={preset} onClick={loadPreset} />
            ))}
          </div>
        </section>
      </main>
    )
  }

  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <div className="dashboard-title">
          <img src={akGamingLogo} alt="" className="dashboard-logo" />
          <div>
            <h1>AK Gaming Dashboard</h1>
            <p>Shortcut board for internal services and public channels.</p>
          </div>
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
              <button
                type="button"
                className="header-icon-btn"
                title="Overwrite with preset"
                aria-label="Overwrite with preset"
                onClick={() => setActiveDialog('preset')}
              >
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
                    <CategoryControlGrid
                      category={category}
                      categoryIndex={categoryIndex}
                      totalCategories={dashboardConfig.categories.length}
                      categoryLayout={categoryLayout}
                      onMove={(direction) => moveCategory(category, direction)}
                      onMoveToIndex={(index) => moveCategoryToIndex(category, index)}
                      onResize={(dimension, delta) => resizeCategory(category, dimension, delta)}
                      onDelete={() => deleteCategory(category)}
                    />
                  )}
                </div>
              </header>
              <div className="shortcut-list">
                {shortcuts.map((shortcut) => {
                  const status = statuses[shortcut.id]?.state ?? 'checking'
                  const hasFailedLogo = failedLogos[shortcut.id] === true

                  return (
                    <ShortcutTile
                      key={shortcut.id}
                      shortcut={shortcut}
                      status={status}
                      failedLogo={hasFailedLogo}
                      isEditMode={isEditMode}
                      draggable
                      setFailedLogo={(siteId) =>
                        setFailedLogos((currentFailedLogos) => ({
                          ...currentFailedLogos,
                          [siteId]: true,
                        }))
                      }
                      onDragStart={(event, siteId) => {
                        if (!isEditMode) {
                          return
                        }

                        event.dataTransfer.setData('text/plain', siteId)
                        event.dataTransfer.effectAllowed = 'move'
                        setDraggingSiteId(siteId)
                      }}
                      onDragEnd={() => setDraggingSiteId(null)}
                      onRemove={removeSiteFromBoard}
                    />
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
                  <h2>Overwrite with preset</h2>
                  <button type="button" onClick={closeDialog}>
                    Close
                  </button>
                </header>
                <p className="dialog-copy">Choosing a preset replaces the current dashboard configuration.</p>
                <div className="setup-preset-grid">
                  {PRESET_ORDER.map((preset) => (
                    <PresetCard key={preset} preset={preset} onClick={loadPreset} />
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
                    const hasFailedLogo = failedLogos[shortcut.id] === true

                    return (
                      <ShortcutTile
                        key={shortcut.id}
                        shortcut={shortcut}
                        failedLogo={hasFailedLogo}
                        isEditMode={false}
                        asOption
                        setFailedLogo={(siteId) =>
                          setFailedLogos((currentFailedLogos) => ({
                            ...currentFailedLogos,
                            [siteId]: true,
                          }))
                        }
                        onSelect={(siteId) => {
                          moveSiteToCategory(siteId, entryTargetCategory)
                          closeDialog()
                        }}
                      />
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
