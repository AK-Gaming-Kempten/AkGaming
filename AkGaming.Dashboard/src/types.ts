export type PresetId = 'vorstand' | 'dev' | 'eventleitung' | 'all' | 'custom'

export interface SiteShortcut {
  id: string
  title: string
  color: string
  url: string
  category: string
  presets: PresetId[]
}

export type StatusState = 'unknown' | 'checking' | 'online' | 'offline'

export interface ShortcutStatus {
  state: StatusState
  checkedAt: number | null
}

export interface Position {
  x: number
  y: number
}
