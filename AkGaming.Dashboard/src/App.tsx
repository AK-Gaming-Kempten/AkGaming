import { useEffect, useEffectEvent, useMemo, useState, type CSSProperties } from 'react'
import { CATEGORY_ORDER, PRESET_LABELS, PRESET_ORDER, SHORTCUTS, isVisibleInPreset } from './data/sites'
import { useLocalStorageState } from './hooks/useLocalStorageState'
import type { PresetId, ShortcutStatus, StatusState } from './types'

const STORAGE_PREFIX = 'akg-dashboard'
const PING_INTERVAL_MS = 300_000
const DEFAULT_PRESET_CATEGORIES: Record<PresetId, string[]> = {
  vorstand: [],
  dev: [],
  eventleitung: [],
  all: [],
  custom: [],
}

const DEFAULT_CUSTOM_CATEGORIES = (() => {
  const categories = new Set<string>(CATEGORY_ORDER)
  for (const shortcut of SHORTCUTS) {
    categories.add(shortcut.category)
  }
  return [...categories]
})()

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
  const [selectedPreset, setSelectedPreset] = useLocalStorageState<PresetId>(`${STORAGE_PREFIX}:preset`, 'vorstand')
  const [hiddenByPreset, setHiddenByPreset] = useLocalStorageState<Record<PresetId, string[]>>(
    `${STORAGE_PREFIX}:hidden`,
    DEFAULT_PRESET_CATEGORIES,
  )
  const [customCategories, setCustomCategories] = useLocalStorageState<string[]>(
    `${STORAGE_PREFIX}:custom-categories`,
    DEFAULT_CUSTOM_CATEGORIES,
  )
  const [customSiteCategory, setCustomSiteCategory] = useLocalStorageState<Record<string, string>>(
    `${STORAGE_PREFIX}:custom-site-category`,
    {},
  )
  const [newCategoryName, setNewCategoryName] = useState('')
  const [hiddenCategorySelection, setHiddenCategorySelection] = useState('')
  const [draggingCustomSiteId, setDraggingCustomSiteId] = useState<string | null>(null)
  const [autoHealthChecks, setAutoHealthChecks] = useLocalStorageState<boolean>(
    `${STORAGE_PREFIX}:auto-health-checks`,
    false,
  )
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
  const [lastCheckedAt, setLastCheckedAt] = useState<number | null>(null)

  const isCustomPreset = selectedPreset === 'custom'
  const visibleShortcuts = SHORTCUTS.filter((shortcut) => isVisibleInPreset(shortcut, selectedPreset))

  const shortcutsByCategory = useMemo(() => {
    const grouped: Record<string, typeof visibleShortcuts> = {}
    for (const shortcut of visibleShortcuts) {
      const category = isCustomPreset ? (customSiteCategory[shortcut.id] ?? shortcut.category) : shortcut.category
      grouped[category] ??= []
      grouped[category].push(shortcut)
    }
    return grouped
  }, [customSiteCategory, isCustomPreset, visibleShortcuts])

  const orderedCategories = useMemo(() => {
    if (isCustomPreset) {
      const customOrdered = [...customCategories]
      for (const category of Object.keys(shortcutsByCategory)) {
        if (!customOrdered.includes(category)) {
          customOrdered.push(category)
        }
      }
      return customOrdered
    }

    const ordered: string[] = []
    for (const category of CATEGORY_ORDER) {
      if (shortcutsByCategory[category]?.length) {
        ordered.push(category)
      }
    }
    for (const category of Object.keys(shortcutsByCategory)) {
      if (!ordered.includes(category)) {
        ordered.push(category)
      }
    }
    return ordered
  }, [customCategories, isCustomPreset, shortcutsByCategory])

  const hiddenCategories = new Set(hiddenByPreset[selectedPreset] ?? [])
  const activeCategories = orderedCategories.filter((category) => !hiddenCategories.has(category))
  const hiddenOrderedCategories = orderedCategories.filter((category) => hiddenCategories.has(category))

  useEffect(() => {
    if (!hiddenCategorySelection) {
      return
    }

    if (!hiddenOrderedCategories.includes(hiddenCategorySelection)) {
      setHiddenCategorySelection('')
    }
  }, [hiddenCategorySelection, hiddenOrderedCategories])

  const runHealthChecks = useEffectEvent(async () => {
    setStatuses((currentStatuses) => {
      const nextStatuses = { ...currentStatuses }
      for (const shortcut of visibleShortcuts) {
        const previous = currentStatuses[shortcut.id]
        nextStatuses[shortcut.id] = {
          state: 'checking',
          checkedAt: previous?.checkedAt ?? null,
        }
      }
      return nextStatuses
    })

    const checkedAt = Date.now()
    const checks: Array<{ id: string; state: StatusState }> = []
    for (const shortcut of visibleShortcuts) {
      const state = await probeShortcut(shortcut.url)
      checks.push({ id: shortcut.id, state })
    }

    setStatuses((currentStatuses) => {
      const nextStatuses = { ...currentStatuses }
      for (const check of checks) {
        nextStatuses[check.id] = {
          state: check.state,
          checkedAt,
        }
      }
      return nextStatuses
    })
    setLastCheckedAt(checkedAt)
  })

  useEffect(() => {
    if (!autoHealthChecks) {
      return
    }

    void runHealthChecks()
    const intervalId = window.setInterval(() => {
      void runHealthChecks()
    }, PING_INTERVAL_MS)

    return () => {
      window.clearInterval(intervalId)
    }
  }, [autoHealthChecks, runHealthChecks, selectedPreset])

  function toggleCategoryHidden(category: string) {
    setHiddenByPreset((currentHidden) => {
      const categorySet = new Set(currentHidden[selectedPreset] ?? [])
      if (categorySet.has(category)) {
        categorySet.delete(category)
      } else {
        categorySet.add(category)
      }

      return {
        ...currentHidden,
        [selectedPreset]: [...categorySet].sort(),
      }
    })
  }

  function restoreHiddenCategory(category: string) {
    setHiddenByPreset((currentHidden) => {
      const categorySet = new Set(currentHidden[selectedPreset] ?? [])
      categorySet.delete(category)
      return {
        ...currentHidden,
        [selectedPreset]: [...categorySet].sort(),
      }
    })
  }

  function resetCurrentPreset() {
    setHiddenByPreset((currentHidden) => ({
      ...currentHidden,
      [selectedPreset]: [],
    }))

    if (selectedPreset === 'custom') {
      setCustomCategories(DEFAULT_CUSTOM_CATEGORIES)
      setCustomSiteCategory({})
      setNewCategoryName('')
    }
  }

  function addCustomCategory() {
    const trimmed = newCategoryName.trim()
    if (!trimmed) {
      return
    }

    setCustomCategories((currentCategories) => {
      if (currentCategories.includes(trimmed)) {
        return currentCategories
      }
      return [...currentCategories, trimmed]
    })
    setNewCategoryName('')
  }

  function moveCustomSiteToCategory(siteId: string, category: string) {
    if (!isCustomPreset) {
      return
    }

    setCustomSiteCategory((currentSiteCategory) => ({
      ...currentSiteCategory,
      [siteId]: category,
    }))
  }

  const lastCheckedLabel =
    lastCheckedAt === null
      ? 'not checked yet'
      : new Intl.DateTimeFormat('de-DE', {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
        }).format(lastCheckedAt)

  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <div>
          <h1>AK Gaming Dashboard</h1>
          <p>Shortcut board for internal services and public channels.</p>
        </div>
        <div className="health-controls">
          <span>Last check: {lastCheckedLabel}</span>
          <button
            type="button"
            onClick={() => setAutoHealthChecks((currentAutoHealthChecks) => !currentAutoHealthChecks)}
            className={autoHealthChecks ? 'health-mode active' : 'health-mode'}
          >
            {autoHealthChecks ? 'Auto checks: On' : 'Auto checks: Off'}
          </button>
          <button type="button" onClick={() => void runHealthChecks()}>
            Refresh status
          </button>
        </div>
      </header>

      <section className="presets">
        {PRESET_ORDER.map((preset) => (
          <button
            key={preset}
            type="button"
            className={preset === selectedPreset ? 'preset active' : 'preset'}
            onClick={() => setSelectedPreset(preset)}
          >
            {PRESET_LABELS[preset]}
          </button>
        ))}
        <button type="button" className="preset reset" onClick={resetCurrentPreset}>
          Reset current preset
        </button>
      </section>

      {isCustomPreset && (
        <section className="custom-controls">
          <input
            type="text"
            value={newCategoryName}
            onChange={(event) => setNewCategoryName(event.currentTarget.value)}
            placeholder="New category name"
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                addCustomCategory()
              }
            }}
          />
          <button type="button" onClick={addCustomCategory}>
            Add category
          </button>
        </section>
      )}

      {hiddenOrderedCategories.length > 0 && (
        <section className="hidden-controls">
          <label htmlFor="hidden-category-select">Hidden categories</label>
          <select
            id="hidden-category-select"
            value={hiddenCategorySelection}
            onChange={(event) => setHiddenCategorySelection(event.currentTarget.value)}
          >
            <option value="">Select category</option>
            {hiddenOrderedCategories.map((category) => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>
          <button
            type="button"
            disabled={hiddenCategorySelection.length === 0}
            onClick={() => {
              restoreHiddenCategory(hiddenCategorySelection)
              setHiddenCategorySelection('')
            }}
          >
            Show
          </button>
        </section>
      )}

      <section className="board-grid">
        {activeCategories.map((category) => {
          const shortcuts = shortcutsByCategory[category] ?? []
          return (
            <article
              key={category}
              className={draggingCustomSiteId ? 'category-column drag-target' : 'category-column'}
              onDragOver={(event) => {
                if (isCustomPreset) {
                  event.preventDefault()
                }
              }}
              onDrop={(event) => {
                if (!isCustomPreset) {
                  return
                }

                event.preventDefault()
                const dataTransferSiteId = event.dataTransfer.getData('text/plain')
                const siteId = dataTransferSiteId || draggingCustomSiteId
                if (siteId) {
                  moveCustomSiteToCategory(siteId, category)
                }
                setDraggingCustomSiteId(null)
              }}
            >
              <header className="category-column-header">
                <h2>{category}</h2>
                <div className="category-column-actions">
                  <span>{shortcuts.length}</span>
                  <button type="button" className="category-hide-btn" onClick={() => toggleCategoryHidden(category)}>
                    Hide
                  </button>
                </div>
              </header>
              <div className="shortcut-list">
                {shortcuts.map((shortcut) => {
                  const status = statuses[shortcut.id]?.state ?? 'checking'
                  const colorStyle = { '--blob-color': shortcut.color } as CSSProperties
                  const hasFailedLogo = failedLogos[shortcut.id] === true

                  return (
                    <a
                      key={shortcut.id}
                      href={shortcut.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="shortcut-item"
                      style={colorStyle}
                      draggable={isCustomPreset}
                      onDragStart={(event) => {
                        if (!isCustomPreset) {
                          return
                        }

                        event.dataTransfer.setData('text/plain', shortcut.id)
                        event.dataTransfer.effectAllowed = 'move'
                        setDraggingCustomSiteId(shortcut.id)
                      }}
                      onDragEnd={() => {
                        setDraggingCustomSiteId(null)
                      }}
                    >
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
                  )
                })}
                {shortcuts.length === 0 && <div className="empty-category">No shortcuts</div>}
              </div>
            </article>
          )
        })}
      </section>
    </main>
  )
}

export default App
