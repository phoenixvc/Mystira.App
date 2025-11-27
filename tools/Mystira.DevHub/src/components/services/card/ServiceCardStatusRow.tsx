import { BuildStatus, ServiceConfig, ServiceStatus } from '../types';
import { formatTimeSince, getHealthIndicator } from '../utils/serviceUtils';

interface ServiceCardStatusRowProps {
  config: ServiceConfig;
  status?: ServiceStatus;
  build?: BuildStatus;
  isRunning: boolean;
  isLoading: boolean;
  statusMsg?: string;
  errorCount: number;
  warningCount: number;
  isBuilding: boolean;
  buildFailed: boolean;
  logsCount: number;
  onPortChange: (port: number) => void;
}

export function ServiceCardStatusRow({
  config,
  status,
  build,
  isRunning,
  isLoading,
  statusMsg,
  errorCount,
  warningCount,
  isBuilding,
  buildFailed,
  logsCount,
  onPortChange,
}: ServiceCardStatusRowProps) {
  return (
    <div className="flex items-center justify-between gap-4 flex-wrap mt-3">
      <div className="flex items-center gap-3 flex-wrap">
        <span
          className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase ${
            isRunning
              ? 'bg-green-600 text-white'
              : isLoading && statusMsg
              ? 'bg-yellow-600 text-white'
              : 'bg-gray-600 text-white'
          }`}
        >
          {isRunning
            ? 'RUN'
            : isLoading && statusMsg
            ? statusMsg.toUpperCase().substring(0, 6)
            : 'STOP'}
        </span>
        {build && build.lastBuildTime && (
          <span
            className="px-2 py-0.5 rounded text-[10px] bg-blue-900/20 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-mono font-semibold"
            title={`Last build: ${new Date(build.lastBuildTime).toLocaleString()}`}
          >
            Last build: {formatTimeSince(build.lastBuildTime)}
          </span>
        )}
        {errorCount > 0 && (
          <span
            className="text-[10px] text-red-500 dark:text-red-400 font-mono"
            title="Error count"
          >
            🔴 {errorCount}
          </span>
        )}
        {warningCount > 0 && (
          <span
            className="text-[10px] text-yellow-500 dark:text-yellow-400 font-mono"
            title="Warning count"
          >
            ⚠️ {warningCount}
          </span>
        )}
        {isRunning && (
          <span
            className="text-lg"
            title={`Service is ${status?.health || 'unknown'}`}
          >
            {getHealthIndicator(status?.health)}
          </span>
        )}
        {status?.portConflict && (
          <span
            className="px-2 py-1 rounded text-sm bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300"
            title="Port conflict detected"
          >
            ⚠ Port {config.port} in use
          </span>
        )}
        {config.port && (
          <div className="flex items-center gap-1.5">
            <span className="text-xs text-gray-500 dark:text-gray-400">:</span>
            <input
              type="number"
              min="1"
              max="65535"
              value={config.port}
              onChange={(e) => {
                const newPort = parseInt(e.target.value, 10);
                if (!isNaN(newPort) && newPort !== config.port) {
                  onPortChange(newPort);
                }
              }}
              onBlur={(e) => {
                const newPort = parseInt(e.target.value, 10);
                if (isNaN(newPort) || newPort < 1 || newPort > 65535) {
                  e.target.value = config.port.toString();
                }
              }}
              disabled={isRunning}
              className="w-16 px-1.5 py-0.5 text-xs border border-gray-400 dark:border-gray-500 rounded bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 font-mono disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-1 focus:ring-blue-500"
              title={isRunning ? 'Stop the service to change port' : 'Edit port number'}
            />
          </div>
        )}
      </div>
    </div>
  );
}

