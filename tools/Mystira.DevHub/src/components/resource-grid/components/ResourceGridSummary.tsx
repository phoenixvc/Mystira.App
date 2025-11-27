interface AzureResource {
  status: 'running' | 'stopped' | 'warning' | 'failed' | 'unknown';
}

interface ResourceGridSummaryProps {
  resources: AzureResource[];
  compact: boolean;
}

export function ResourceGridSummary({ resources, compact }: ResourceGridSummaryProps) {
  const running = resources.filter((r) => r.status === 'running').length;
  const stopped = resources.filter((r) => r.status === 'stopped').length;
  const warnings = resources.filter((r) => r.status === 'warning').length;
  const failed = resources.filter((r) => r.status === 'failed').length;

  return (
    <div className={`flex items-center gap-4 ${compact ? 'text-xs' : 'text-sm'} text-gray-600 dark:text-gray-400`}>
      <div className="flex items-center gap-1">
        <span className={`font-bold text-green-700 dark:text-green-400 ${compact ? '' : 'text-xl'}`}>{running}</span>
        <span className="text-gray-600 dark:text-gray-400">Running</span>
      </div>
      <div className="flex items-center gap-1">
        <span className={`font-bold text-gray-700 dark:text-gray-300 ${compact ? '' : 'text-xl'}`}>{stopped}</span>
        <span className="text-gray-600 dark:text-gray-400">Stopped</span>
      </div>
      <div className="flex items-center gap-1">
        <span className={`font-bold text-yellow-700 dark:text-yellow-400 ${compact ? '' : 'text-xl'}`}>
          {warnings}
        </span>
        <span className="text-gray-600 dark:text-gray-400">Warnings</span>
      </div>
      <div className="flex items-center gap-1">
        <span className={`font-bold text-red-700 dark:text-red-400 ${compact ? '' : 'text-xl'}`}>{failed}</span>
        <span className="text-gray-600 dark:text-gray-400">Failed</span>
      </div>
    </div>
  );
}

