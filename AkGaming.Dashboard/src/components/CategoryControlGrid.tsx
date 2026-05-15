import type { CategoryLayout } from '../dashboard/types'

interface CategoryControlGridProps {
  category: string
  categoryIndex: number
  totalCategories: number
  categoryLayout: CategoryLayout
  onMove: (direction: -1 | 1) => void
  onMoveToIndex: (index: number) => void
  onResize: (dimension: keyof CategoryLayout, delta: -1 | 1) => void
  onDelete: () => void
}

export function CategoryControlGrid({
  category,
  categoryIndex,
  totalCategories,
  categoryLayout,
  onMove,
  onMoveToIndex,
  onResize,
  onDelete,
}: CategoryControlGridProps) {
  return (
    <div className="category-control-grid" aria-label={`${category} layout controls`}>
      <button type="button" className="category-tool-btn" title="Move earlier" aria-label="Move earlier" disabled={categoryIndex <= 0} onClick={() => onMove(-1)}>
        <span className="bi bi-caret-left-fill" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Shorter"
        aria-label="Shorter"
        disabled={categoryLayout.rowSpan <= 1}
        onClick={() => onResize('rowSpan', -1)}
      >
        <span className="bi bi-dash-lg" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Move later"
        aria-label="Move later"
        disabled={categoryIndex >= totalCategories - 1}
        onClick={() => onMove(1)}
      >
        <span className="bi bi-caret-right-fill" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Narrower"
        aria-label="Narrower"
        disabled={categoryLayout.colSpan <= 1}
        onClick={() => onResize('colSpan', -1)}
      >
        <span className="bi bi-dash-lg" aria-hidden="true"></span>
      </button>
      <button type="button" className="category-tool-btn" title="Delete category" aria-label="Delete category" onClick={onDelete}>
        <span className="bi bi-x-lg" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Wider"
        aria-label="Wider"
        disabled={categoryLayout.colSpan >= 3}
        onClick={() => onResize('colSpan', 1)}
      >
        <span className="bi bi-plus-lg" aria-hidden="true"></span>
      </button>
      <button type="button" className="category-tool-btn" title="Move first" aria-label="Move first" disabled={categoryIndex <= 0} onClick={() => onMoveToIndex(0)}>
        <span className="bi bi-caret-up-fill" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Taller"
        aria-label="Taller"
        disabled={categoryLayout.rowSpan >= 3}
        onClick={() => onResize('rowSpan', 1)}
      >
        <span className="bi bi-plus-lg" aria-hidden="true"></span>
      </button>
      <button
        type="button"
        className="category-tool-btn"
        title="Move last"
        aria-label="Move last"
        disabled={categoryIndex >= totalCategories - 1}
        onClick={() => onMoveToIndex(totalCategories - 1)}
      >
        <span className="bi bi-caret-down-fill" aria-hidden="true"></span>
      </button>
    </div>
  )
}
