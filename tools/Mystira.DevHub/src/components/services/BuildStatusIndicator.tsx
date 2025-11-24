import { BuildStatus } from './types';

interface BuildStatusIndicatorProps {
  build: BuildStatus;
  formatTimeSince: (timestamp?: number) => string | null;
}

export function BuildStatusIndicator({ build, formatTimeSince }: BuildStatusIndicatorProps) {
  return (
    <div className="px-4 pt-3 pb-2 border-b border-gray-200 dark:border-gray-700">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {build.status === 'building' && (
            <div className="flex items-center gap-2">
              <div className="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></div>
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {build.message || 'Building...'}
              </span>
              {build.progress !== undefined && (
                <span className="text-xs text-gray-500 dark:text-gray-500">
                  {build.progress}%
                </span>
              )}
            </div>
          )}
          {build.status === 'success' && (
            <div className="flex items-center gap-2">
              <span className="text-green-600 dark:text-green-400">✓</span>
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {build.message || 'Build successful'}
              </span>
              {build.lastBuildTime && (
                <span className="text-xs text-gray-500 dark:text-gray-500">
                  {formatTimeSince(build.lastBuildTime)}
                </span>
              )}
            </div>
          )}
          {build.status === 'failed' && (
            <div className="flex items-center gap-2">
              <span className="text-red-600 dark:text-red-400">✗</span>
              <span className="text-sm text-red-600 dark:text-red-400">
                {build.message || 'Build failed'}
              </span>
              {build.lastBuildTime && (
                <span className="text-xs text-gray-500 dark:text-gray-500">
                  {formatTimeSince(build.lastBuildTime)}
                </span>
              )}
            </div>
          )}
        </div>
        {build.buildDuration && (
          <span className="text-xs text-gray-500 dark:text-gray-500">
            Duration: {(build.buildDuration / 1000).toFixed(1)}s
          </span>
        )}
      </div>
      {build.status === 'building' && build.progress !== undefined && (
        <div className="mt-2 w-full bg-gray-200 dark:bg-gray-700 rounded-full h-1.5">
          <div
            className="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
            style={{ width: `${build.progress}%` }}
          ></div>
        </div>
      )}
    </div>
  );
}

