import type { CSSProperties } from 'react'
import { PRESET_LABELS } from '../data/sites'
import { PRESET_STYLE } from '../dashboard/config'
import type { PresetId } from '../types'

interface PresetCardProps {
  preset: PresetId
  onClick: (preset: PresetId) => void
}

export function PresetCard({ preset, onClick }: PresetCardProps) {
  const presetStyle = PRESET_STYLE[preset]

  return (
    <button
      type="button"
      className="preset-card"
      style={{ '--preset-color': presetStyle.color } as CSSProperties}
      onClick={() => onClick(preset)}
    >
      <span className="preset-card-icon">
        <span className={`bi ${presetStyle.icon}`} aria-hidden="true"></span>
      </span>
      <span className="preset-card-label">{PRESET_LABELS[preset]}</span>
    </button>
  )
}
