import { formatTimeSince } from '../../services/utils/serviceUtils';

interface ProjectDeploymentPlannerHeaderProps {
  lastRefreshTime: number | null;
  loadingStatus: boolean;
  onRefresh: () => void;
  selectedCount?: number;
  totalCount?: number;
  onSelectAll?: () => void;
  onDeselectAll?: () => void;
}

export function ProjectDeploymentPlannerHeader({
  lastRefreshTime,
  loadingStatus,
  onRefresh,
  selectedCount = 0,
  totalCount = 0,
  onSelectAll,
  onDeselectAll,
}: ProjectDeploymentPlannerHeaderProps) {
  const allSelected = selectedCount === totalCount && totalCount > 0;

  return (
    <div className="flex flex-col gap-3 mb-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1 flex items-center gap-2">
            Step 1: Plan Infrastructure Deployment
            <span
              className="text-sm font-normal text-gray-500 dark:text-gray-400 cursor-help"
              title="Select the infrastructure templates you want to deploy. Each template creates the necessary Azure resources for the corresponding project."
            >
              ⓘ
            </span>
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Select infrastructure templates for each project that needs cloud deployment
          </p>
        </div>
        <div className="flex items-center gap-3">
          {lastRefreshTime && (
            <div className="text-xs text-gray-500 dark:text-gray-400">
              Last refreshed: {formatTimeSince(lastRefreshTime)}
            </div>
          )}
          <button
            onClick={onRefresh}
            disabled={loadingStatus}
            className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded disabled:opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
          >
            {loadingStatus ? '🔄 Loading...' : '🔄 Refresh Status'}
          </button>
        </div>
      </div>

      {/* Selection controls */}
      {totalCount > 0 && (
        <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-600 dark:text-gray-400">
              {selectedCount > 0 ? (
                <>
                  <span className="font-semibold text-blue-600 dark:text-blue-400">{selectedCount}</span>
                  <span> of {totalCount} templates selected</span>
                </>
              ) : (
                <span className="text-amber-600 dark:text-amber-400">No templates selected - select at least one to continue</span>
              )}
            </span>
          </div>
          <div className="flex items-center gap-2">
            {onSelectAll && (
              <button
                onClick={onSelectAll}
                disabled={allSelected}
                className="px-3 py-1.5 text-xs bg-blue-600 dark:bg-blue-500 hover:bg-blue-700 dark:hover:bg-blue-600 text-white rounded disabled:opacity-50 disabled:cursor-not-allowed transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
                title="Select all infrastructure templates"
              >
                Select All
              </button>
            )}
            {onDeselectAll && (
              <button
                onClick={onDeselectAll}
                disabled={selectedCount === 0}
                className="px-3 py-1.5 text-xs bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded disabled:opacity-50 disabled:cursor-not-allowed transition-colors focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
                title="Deselect all infrastructure templates"
              >
                Deselect All
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

