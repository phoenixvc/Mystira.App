import { ServiceConfig, ServiceStatus } from './types';
import { getHealthIndicator } from './utils/serviceUtils';

interface ServiceControlsProps {
  config: ServiceConfig;
  status?: ServiceStatus;
  isRunning: boolean;
  isLoading: boolean;
  statusMsg?: string;
  onStart: () => void;
  onStop: () => void;
  onPortChange: (port: number) => void;
}

export function ServiceControls({
  config,
  status,
  isRunning,
  isLoading,
  statusMsg,
  onStart,
  onStop,
  onPortChange,
}: ServiceControlsProps) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex-1">
        <div className="flex items-center gap-3 flex-wrap">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white">{config.displayName}</h3>
          <span
            className={`px-2 py-1 rounded text-sm ${
              isRunning
                ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
                : isLoading && statusMsg
                ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
            }`}
          >
            {isRunning ? 'Running' : isLoading && statusMsg ? statusMsg : 'Stopped'}
          </span>
          {isRunning && (
            <span className="text-lg" title={`Service is ${status?.health || 'unknown'}`}>
              {getHealthIndicator(status?.health)}
            </span>
          )}
          {status?.portConflict && (
            <span className="px-2 py-1 rounded text-sm bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300" title="Port conflict detected">
              ⚠ Port {config.port} in use
            </span>
          )}
          {config.port && (
            <div className="flex items-center gap-2">
              <span className="text-sm text-gray-600 dark:text-gray-400">Port:</span>
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
                className="w-20 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-blue-500"
                title={isRunning ? "Stop the service to change port" : "Edit port number"}
              />
            </div>
          )}
        </div>
      </div>
      <div>
        {isRunning ? (
          <button
            onClick={onStop}
            disabled={isLoading}
            className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 disabled:opacity-50"
          >
            {isLoading ? 'Stopping...' : 'Stop'}
          </button>
        ) : (
          <button
            onClick={onStart}
            disabled={isLoading || status?.portConflict}
            className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:opacity-50"
            title={status?.portConflict ? `Port ${config.port} is already in use` : ''}
          >
            {isLoading ? (statusMsg || 'Starting...') : 'Start'}
          </button>
        )}
      </div>
    </div>
  );
}

