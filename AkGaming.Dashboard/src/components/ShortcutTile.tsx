import type { CSSProperties, DragEvent } from 'react'
import { buildLogoUrl, shortcutInitials } from '../dashboard/shortcuts'
import type { SiteShortcut, StatusState } from '../types'

interface ShortcutTileProps {
  shortcut: SiteShortcut
  status?: StatusState
  failedLogo: boolean
  setFailedLogo: (id: string) => void
  isEditMode: boolean
  onRemove?: (id: string) => void
  draggable?: boolean
  onDragStart?: (event: DragEvent<HTMLDivElement>, siteId: string) => void
  onDragEnd?: () => void
  asOption?: boolean
  onSelect?: (id: string) => void
}

export function ShortcutTile({
  shortcut,
  status = 'unknown',
  failedLogo,
  setFailedLogo,
  isEditMode,
  onRemove,
  draggable = false,
  onDragStart,
  onDragEnd,
  asOption = false,
  onSelect,
}: ShortcutTileProps) {
  const colorStyle = { '--blob-color': shortcut.color } as CSSProperties

  return (
    <div
      className={`shortcut-item${asOption ? ' unused-entry-option' : ''}`}
      style={colorStyle}
      draggable={draggable && isEditMode}
      onDragStart={(event) => onDragStart?.(event, shortcut.id)}
      onDragEnd={onDragEnd}
    >
      {asOption ? (
        <button type="button" className="shortcut-link" onClick={() => onSelect?.(shortcut.id)}>
          {failedLogo ? (
            <span className="logo-fallback">{shortcutInitials(shortcut.title)}</span>
          ) : (
            <img
              src={buildLogoUrl(shortcut.url)}
              alt=""
              className="logo"
              loading="lazy"
              onError={() => {
                setFailedLogo(shortcut.id)
              }}
            />
          )}
          <span className="shortcut-title">{shortcut.title}</span>
        </button>
      ) : (
        <>
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
            {failedLogo ? (
              <span className="logo-fallback">{shortcutInitials(shortcut.title)}</span>
            ) : (
              <img
                src={buildLogoUrl(shortcut.url)}
                alt=""
                className="logo"
                loading="lazy"
                onError={() => {
                  setFailedLogo(shortcut.id)
                }}
              />
            )}
            <span className="shortcut-title">{shortcut.title}</span>
          </a>
          {isEditMode && onRemove && (
            <button type="button" className="shortcut-remove" title="Remove entry" aria-label="Remove entry" onClick={() => onRemove(shortcut.id)}>
              <span className="bi bi-x-lg" aria-hidden="true"></span>
            </button>
          )}
        </>
      )}
    </div>
  )
}
