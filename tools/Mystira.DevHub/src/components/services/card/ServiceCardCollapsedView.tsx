import { BuildStatus, ServiceConfig, ServiceStatus } from '../types';
import { formatTimeSince, getHealthIndicator } from '../utils/serviceUtils';

interface ServiceCardCollapsedViewProps {
  config: ServiceConfig;
  status?: ServiceStatus;
  build?: BuildStatus;
  isRunning: boolean;
  isLoading: boolean;
  statusMsg?: string;
  isBuilding: boolean;
  buildFailed: boolean;
  logsCount: number;
  errorCount: number;
  warningCount: number;
}

export function ServiceCardCollapsedView({
  config,
  status,
  build,
  isRunning,
  isLoading,
  statusMsg,
  isBuilding,
  buildFailed,
  logsCount,
  errorCount,
  warningCount,
}: ServiceCardCollapsedViewProps) {
  return (
    <div className="mt-1 pt-1 border-t border-gray-300 dark:border-gray-600 flex items-center gap-1.5 text-[10px] text-gray-500 dark:text-gray-400 font-mono flex-wrap">
      <span
        className={`px-1 py-0.5 rounded text-[9px] font-bold uppercase ${
          isRunning
            ? 'bg-green-600 text-white'
            : isLoading && statusMsg
            ? 'bg-yellow-600 text-white'
            : 'bg-gray-600 text-white'
        }`}
      >
        {isRunning ? 'RUN' : isLoading && statusMsg ? statusMsg.toUpperCase().substring(0, 6) : 'STOP'}
      </span>
      {isRunning && status?.health && (
        <span title={`Service is ${status.health}`} className="text-sm">
          {getHealthIndicator(status.health)}
        </span>
      )}
      {config.port && <span className="text-gray-600 dark:text-gray-300">:{config.port}</span>}
      {status?.portConflict && (
        <span className="text-yellow-500 dark:text-yellow-400 text-[10px]">⚠ CONFLICT</span>
      )}
      {build && build.status === 'building' && (
        <span className="text-blue-500 dark:text-blue-400 text-[10px] animate-pulse">
          {build.isManual ? '[REBUILDING]' : '[BUILDING]'}
        </span>
      )}
      {build && build.status === 'failed' && (
        <span className="text-red-500 dark:text-red-400 text-[10px]">
          {build.isManual ? '[REBUILD FAIL]' : '[FAILED]'}
        </span>
      )}
      {build && build.lastBuildTime && (
        <span
          className="px-1.5 py-0.5 rounded text-[10px] bg-blue-900/20 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-semibold"
          title={`Last build: ${new Date(build.lastBuildTime).toLocaleString()}`}
        >
          Built: {formatTimeSince(build.lastBuildTime)}
        </span>
      )}
      {(isBuilding || buildFailed || logsCount > 0) && (
        <span
          className="px-1.5 py-0.5 rounded text-[10px] bg-blue-900/30 dark:bg-blue-900/40 text-blue-400 dark:text-blue-300 font-semibold flex items-center gap-1 animate-pulse"
          title="Logs available - Click to expand"
        >
          <span className="w-1.5 h-1.5 bg-blue-400 rounded-full"></span>
          {isBuilding
            ? build?.isManual
              ? 'REBUILDING'
              : 'BUILDING'
            : buildFailed
            ? build?.isManual
              ? 'REBUILD FAIL'
              : 'FAILED'
            : logsCount > 0
            ? `${logsCount} logs`
            : ''}
        </span>
      )}
      {errorCount > 0 && (
        <span className="text-red-500 dark:text-red-400 text-[10px] font-bold" title="Error count">
          🔴 {errorCount}
        </span>
      )}
      {warningCount > 0 && (
        <span className="text-yellow-500 dark:text-yellow-400 text-[10px] font-bold" title="Warning count">
          ⚠️ {warningCount}
        </span>
      )}
    </div>
  );
}

