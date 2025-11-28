import { formatTimeSince } from '../../services/utils/serviceUtils';

interface ProjectDeploymentPlannerHeaderProps {
  lastRefreshTime: number | null;
  loadingStatus: boolean;
  onRefresh: () => void;
}

export function ProjectDeploymentPlannerHeader({
  lastRefreshTime,
  loadingStatus,
  onRefresh,
}: ProjectDeploymentPlannerHeaderProps) {
  return (
    <div className="flex items-center justify-between mb-4">
      <div>
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
          Step 1: Plan Infrastructure Deployment
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
          className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded disabled:opacity-50"
        >
          {loadingStatus ? '🔄 Loading...' : '🔄 Refresh Status'}
        </button>
      </div>
    </div>
  );
}

