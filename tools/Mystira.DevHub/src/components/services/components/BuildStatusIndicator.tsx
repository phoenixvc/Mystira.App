import { BuildStatus } from '../types';

interface BuildStatusIndicatorProps {
  build: BuildStatus;
  formatTimeSince: (timestamp?: number) => string | null;
}

export function BuildStatusIndicator({ build, formatTimeSince }: BuildStatusIndicatorProps) {
  return (
    <div className="px-3 py-1.5 border-b border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-800 font-mono">
      <div className="flex items-center justify-between text-xs">
        <div className="flex items-center gap-2">
          {build.status === 'building' && (
            <div className="flex items-center gap-2">
              <div className="animate-spin h-3 w-3 border-2 border-blue-500 border-t-transparent rounded-full"></div>
              <span className="text-blue-600 dark:text-blue-400 font-bold">
                {build.isManual ? '[REBUILDING]' : '[BUILDING]'}
              </span>
              <span className="text-gray-600 dark:text-gray-400">
                {build.message || (build.isManual ? 'Rebuilding...' : 'Building...')}
              </span>
              {build.progress !== undefined && (
                <span className="text-gray-500 dark:text-gray-500">
                  {build.progress}%
                </span>
              )}
            </div>
          )}
          {build.status === 'success' && (
            <div className="flex items-center gap-2">
              <span className="text-green-600 dark:text-green-400 font-bold">[OK]</span>
              <span className="text-gray-600 dark:text-gray-400">
                {build.message || (build.isManual ? 'Rebuild successful' : 'Build successful')}
              </span>
              {build.lastBuildTime && (
                <span className="text-blue-600 dark:text-blue-400 font-semibold">
                  {build.isManual ? 'Rebuilt' : 'Built'}: {formatTimeSince(build.lastBuildTime)}
                </span>
              )}
            </div>
          )}
          {build.status === 'failed' && (
            <div className="flex items-center gap-2">
              <span className="text-red-600 dark:text-red-400 font-bold">
                {build.isManual ? '[REBUILD FAIL]' : '[FAIL]'}
              </span>
              <span className="text-red-600 dark:text-red-400">
                {build.message || (build.isManual ? 'Rebuild failed' : 'Build failed')}
              </span>
              {build.lastBuildTime && (
                <span className="text-gray-500 dark:text-gray-500">
                  {formatTimeSince(build.lastBuildTime)}
                </span>
              )}
            </div>
          )}
        </div>
        {build.buildDuration && (
          <span className="text-gray-500 dark:text-gray-500">
            {(build.buildDuration / 1000).toFixed(1)}s
          </span>
        )}
      </div>
      {build.status === 'building' && build.progress !== undefined && (
        <div className="mt-1.5 w-full bg-gray-300 dark:bg-gray-700 rounded-full h-1">
          <div
            className="bg-blue-500 h-1 rounded-full transition-all duration-300"
            style={{ width: `${build.progress}%` }}
          ></div>
        </div>
      )}
    </div>
  );
}

