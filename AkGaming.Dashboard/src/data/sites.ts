import type { PresetId, SiteShortcut } from '../types'

export const PRESET_ORDER: PresetId[] = ['vorstand', 'dev', 'eventleitung', 'all', 'custom']

export const PRESET_LABELS: Record<PresetId, string> = {
  vorstand: 'Vorstand',
  dev: 'Dev',
  eventleitung: 'Eventleitung',
  all: 'All',
  custom: 'Custom',
}

export const CATEGORY_ORDER = [
  'Public',
  'Management',
  'Development',
  'Infrastructure',
  'Collaboration',
  'Security',
] as const

export const SHORTCUTS: SiteShortcut[] = [
  { id: 'website', title: 'Website', color: '#131', url: 'https://akgaming.de', category: 'Public', presets: ['vorstand', 'eventleitung', 'all'] },
  { id: 'itch', title: 'Itch.io', color: '#311', url: 'https://ak-gaming-ev.itch.io/', category: 'Public', presets: ['eventleitung', 'all'] },
  { id: 'website-test', title: 'Website (Test)', color: '#040', url: 'https://test.akgaming.de', category: 'Development', presets: ['dev', 'all'] },
  { id: 'identity', title: 'Identity', color: '#204', url: 'https://identity.akgaming.de', category: 'Management', presets: ['vorstand', 'dev', 'all'] },
  { id: 'management-test', title: 'Management (Test)', color: '#420', url: 'https://management.test.akgaming.de', category: 'Development', presets: ['dev', 'all'] },
  { id: 'management', title: 'Management', color: '#420', url: 'https://management.akgaming.de', category: 'Management', presets: ['vorstand', 'all'] },
  { id: 'identity-test', title: 'Identity (Test)', color: '#204', url: 'https://identity.test.akgaming.de', category: 'Development', presets: ['dev', 'all'] },
  { id: 'github', title: 'GitHub', color: '#737', url: 'https://github.com/AK-Gaming-Kempten/AkGaming/', category: 'Development', presets: ['vorstand', 'dev', 'all'] },
  { id: 'drive', title: 'Drive', color: '#550', url: 'https://drive.google.com/drive/folders/134CF8IHMDB5f_tY3vdUzW1ZFn3yDfI6_', category: 'Collaboration', presets: ['vorstand', 'eventleitung', 'all'] },
  { id: 'kanban', title: 'Kanban', color: '#024', url: 'https://kanban.akgaming.de', category: 'Management', presets: ['vorstand', 'dev', 'eventleitung', 'all'] },
  { id: 'twitch', title: 'Twitch', color: '#205', url: 'https://www.twitch.tv/akgamingkempten', category: 'Public', presets: ['eventleitung', 'all'] },
  { id: 'hetzner', title: 'Hetzner', color: '#ddd', url: 'https://console.hetzner.com/projects/10897922/servers/65426787/overview', category: 'Infrastructure', presets: ['dev', 'all'] },
  { id: 'coolify', title: 'Coolify', color: '#000', url: 'https://coolify.akgaming.de', category: 'Infrastructure', presets: ['dev', 'all'] },
  { id: 'bitwarden', title: 'Bitwarden', color: '#025', url: 'https://bitwarden.eu', category: 'Security', presets: ['vorstand', 'dev', 'all'] },
  { id: 'vaultwarden', title: 'Vaultwarden', color: '#55a', url: 'https://keyvault.akgaming.de', category: 'Security', presets: ['vorstand', 'dev', 'all'] },
]

export function isVisibleInPreset(shortcut: SiteShortcut, preset: PresetId) {
  if (preset === 'all' || preset === 'custom') {
    return true
  }

  return shortcut.presets.includes(preset)
}
