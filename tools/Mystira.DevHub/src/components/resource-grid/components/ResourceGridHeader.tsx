interface ResourceGridHeaderProps {
  resourcesCount: number;
  compact: boolean;
  groupByType: boolean;
  viewMode: 'grid' | 'table';
  onRefresh?: () => void;
  onViewModeChange: (mode: 'grid' | 'table') => void;
  onGroupByTypeChange: (group: boolean) => void;
}

export function ResourceGridHeader({
  resourcesCount,
  compact,
  groupByType,
  viewMode,
  onRefresh,
  onViewModeChange,
  onGroupByTypeChange,
}: ResourceGridHeaderProps) {
  return (
    <div className={`flex items-center justify-between ${compact ? 'mb-2' : 'mb-4'}`}>
      <h3 className={`font-semibold text-gray-900 dark:text-white ${compact ? 'text-sm' : ''}`}>
        Azure Resources ({resourcesCount})
      </h3>
      <div className="flex items-center gap-2">
        {groupByType && (
          <div className="flex gap-1">
            <button
              onClick={() => {
                const groups = document.querySelectorAll('[data-group-key]');
                groups.forEach((g) => {
                  const key = g.getAttribute('data-group-key');
                  if (key) {
                    const button = g.querySelector('button');
                    if (button && button.textContent?.includes('▶')) {
                      button.click();
                    }
                  }
                });
              }}
              className="px-2 py-1 text-[10px] bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 rounded transition-colors"
              title="Collapse all groups"
            >
              ▲
            </button>
            <button
              onClick={() => {
                const groups = document.querySelectorAll('[data-group-key]');
                groups.forEach((g) => {
                  const key = g.getAttribute('data-group-key');
                  if (key) {
                    const button = g.querySelector('button');
                    if (button && button.textContent?.includes('▼')) {
                      button.click();
                    }
                  }
                });
              }}
              className="px-2 py-1 text-[10px] bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 rounded transition-colors"
              title="Expand all groups"
            >
              ▼
            </button>
          </div>
        )}
        {!groupByType && (
          <div className="flex items-center bg-gray-100 dark:bg-gray-800 rounded p-0.5">
            <button
              onClick={() => onViewModeChange('grid')}
              className={`px-2 py-1 text-xs rounded transition-colors ${
                viewMode === 'grid'
                  ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                  : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
              }`}
              title="Grid view"
            >
              ▦
            </button>
            <button
              onClick={() => onViewModeChange('table')}
              className={`px-2 py-1 text-xs rounded transition-colors ${
                viewMode === 'table'
                  ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                  : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
              }`}
              title="Table view"
            >
              ≡
            </button>
          </div>
        )}
        {onRefresh && (
          <button
            onClick={onRefresh}
            className={`${compact ? 'text-[10px] px-2 py-1' : 'text-sm px-3 py-1'} bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 rounded transition-colors`}
          >
            🔄 {compact ? '' : 'Refresh'}
          </button>
        )}
      </div>
    </div>
  );
}

