export interface CategoryLayout {
  colSpan: number
  rowSpan: number
}

export interface DashboardConfig {
  categories: string[]
  siteCategory: Record<string, string>
  unusedSiteIds: string[]
  hiddenCategories?: string[]
  categoryLayout?: Record<string, CategoryLayout>
}

export type ActiveDialog = 'preset' | 'category' | 'entry' | null
