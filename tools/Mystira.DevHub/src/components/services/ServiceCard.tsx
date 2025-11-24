import { useEffect, useState } from 'react';
import { BuildStatusIndicator } from './BuildStatusIndicator';
import { formatDeploymentInfo, type DeploymentInfo } from './DeploymentInfo';
import { EnvironmentSwitcher } from './EnvironmentSwitcher';
import { LogsViewer } from './LogsViewer';
import { ViewModeSelector } from './ViewModeSelector';
import { WebviewView } from './WebviewView';
import { BuildStatus, ServiceConfig, ServiceLog, ServiceStatus } from './types';
import { formatTimeSince, getHealthIndicator } from './utils/serviceUtils';

interface ServiceCardProps {
  config: ServiceConfig;
  status?: ServiceStatus;
  build?: BuildStatus;
  isRunning: boolean;
  isLoading: boolean;
  statusMsg?: string;
  serviceLogs: ServiceLog[];
  logs: ServiceLog[];
  filter: { search: string; type: 'all' | 'stdout' | 'stderr' };
  isAutoScroll: boolean;
  viewMode: 'logs' | 'webview' | 'split';
  isMaximized: boolean;
  webviewError: boolean;
  currentEnv: 'local' | 'dev' | 'prod';
  envUrls: { dev?: string; prod?: string };
  environmentStatus?: { dev?: 'online' | 'offline' | 'checking'; prod?: 'online' | 'offline' | 'checking' };
  deploymentInfo?: DeploymentInfo | null;
  onStart: () => void;
  onStop: () => void;
  onPortChange: (port: number) => void;
  onEnvironmentSwitch: (env: 'local' | 'dev' | 'prod') => void;
  onViewModeChange: (mode: 'logs' | 'webview' | 'split') => void;
  onMaximize: () => void;
  onOpenInBrowser: (url: string) => void;
  onOpenInTauriWindow: (url: string, title: string) => void;
  onClearLogs: () => void;
  onFilterChange: (filter: { search: string; type: 'all' | 'stdout' | 'stderr' }) => void;
  onAutoScrollChange: (enabled: boolean) => void;
  onWebviewRetry: () => void;
  onWebviewError: () => void;
  maxLogs?: number;
  onMaxLogsChange?: (limit: number) => void;
}

export function ServiceCard({
  config,
  status,
  build,
  isRunning,
  isLoading,
  statusMsg,
  serviceLogs,
  logs,
  filter,
  isAutoScroll,
  viewMode,
  isMaximized,
  webviewError,
  currentEnv,
  envUrls,
  environmentStatus,
  deploymentInfo,
  onStart,
  onStop,
  onPortChange,
  onEnvironmentSwitch,
  onViewModeChange,
  onMaximize,
  onOpenInBrowser,
  onOpenInTauriWindow,
  onClearLogs,
  onFilterChange,
  onAutoScrollChange,
  onWebviewRetry,
  onWebviewError,
  maxLogs,
  onMaxLogsChange,
}: ServiceCardProps) {
  const [isCollapsed, setIsCollapsed] = useState(() => {
    const saved = localStorage.getItem(`service-${config.name}-collapsed`);
    return saved === 'true';
  });

  const isBuilding = build && build.status === 'building';
  const buildFailed = build && build.status === 'failed';
  const hasImportantLogs = logs.length > 0 || isBuilding || buildFailed;

  // Auto-expand if building, failed, or has logs
  useEffect(() => {
    if ((isBuilding || buildFailed || logs.length > 0) && isCollapsed) {
      setIsCollapsed(false);
    }
  }, [isBuilding, buildFailed, logs.length, isCollapsed]);

  const toggleCollapse = () => {
    const newState = !isCollapsed;
    setIsCollapsed(newState);
    localStorage.setItem(`service-${config.name}-collapsed`, String(newState));
  };

  const currentViewMode = (isBuilding || buildFailed) ? 'logs' : viewMode;
  const containerClass = isMaximized ? 'h-[calc(100vh-60px)]' : 'max-h-96';
  // Always show view if there are logs, building, or running
  const showView = hasImportantLogs || (isRunning && config.url);

  const logsViewProps = {
    serviceName: config.name,
    logs,
    filteredLogs: serviceLogs,
    filter,
    isAutoScroll,
    isMaximized,
    containerClass,
    maxLogs,
    onFilterChange,
    onAutoScrollChange,
    onMaxLogsChange,
  };

  const webviewViewProps = {
    config,
    hasError: webviewError,
    isMaximized,
    containerClass,
    onRetry: onWebviewRetry,
    onOpenInTauriWindow,
    onOpenInBrowser,
    onError: onWebviewError,
  };

  return (
    <div className={`border border-gray-300 dark:border-gray-600 rounded-md bg-gray-50 dark:bg-gray-900 shadow-sm transition-all font-mono ${
      currentEnv === 'prod' 
        ? 'border-l-4 border-red-500 bg-red-950/20 dark:bg-red-950/30' 
        : currentEnv === 'dev' 
        ? 'border-l-4 border-blue-500 bg-blue-950/20 dark:bg-blue-950/30' 
        : 'border-l-4 border-green-500 bg-green-950/10 dark:bg-green-950/20'
    }`}>
      {/* Build Status Indicator - Always visible if building or failed */}
      {build && (build.status === 'building' || build.status === 'failed') && (
        <BuildStatusIndicator build={build} formatTimeSince={formatTimeSince} />
      )}
      
      <div className="p-3">
        {/* Top Row: Service Name + Environment Badge | Start/Stop Button */}
        <div className="flex items-center justify-between gap-3">
          <button
            onClick={toggleCollapse}
            className="flex items-center gap-2 flex-1 text-left hover:opacity-80 transition-opacity group"
            title={isCollapsed ? 'Expand service details' : 'Collapse service details'}
          >
            <span className="text-gray-500 dark:text-gray-400 text-xs group-hover:text-gray-700 dark:group-hover:text-gray-300 transition-colors">
              {isCollapsed ? '▶' : '▼'}
            </span>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-bold text-gray-900 dark:text-gray-100 tracking-tight">
                {config.displayName}
              </h3>
            <span className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
              currentEnv === 'local' 
                ? 'bg-green-600 text-white' 
                : currentEnv === 'dev'
                ? 'bg-blue-600 text-white'
                : 'bg-red-600 text-white'
            }`}>
              {currentEnv === 'local' ? 'LOCAL' : currentEnv === 'dev' ? 'DEV' : 'PROD'}
            </span>
              {currentEnv !== 'local' && environmentStatus && (
                <span className="text-xs" title={`Environment status: ${environmentStatus[currentEnv] || 'unknown'}`}>
                  {environmentStatus[currentEnv] === 'online' ? '🟢' : 
                   environmentStatus[currentEnv] === 'offline' ? '🔴' : 
                   environmentStatus[currentEnv] === 'checking' ? '🟡' : '⚪'}
                </span>
              )}
            </div>
          </button>
          {/* Start/Stop Button - Right Aligned */}
          <div className="flex items-center gap-2">
            {/* Status Badge - Always visible when collapsed */}
            {isCollapsed && (
              <span
                className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase ${
                  isRunning
                    ? 'bg-green-600 text-white'
                    : isLoading && statusMsg
                    ? 'bg-yellow-600 text-white'
                    : 'bg-gray-600 text-white'
                }`}
              >
                {isRunning ? 'RUN' : isLoading && statusMsg ? statusMsg.toUpperCase().substring(0, 6) : 'STOP'}
              </span>
            )}
            {isRunning ? (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onStop();
                }}
                disabled={isLoading}
                className="px-3 py-1.5 bg-red-600 text-white rounded text-xs font-bold hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors uppercase tracking-wide"
              >
                {isLoading ? 'STOPPING' : 'STOP'}
              </button>
            ) : (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onStart();
                }}
                disabled={isLoading || status?.portConflict}
                className="px-3 py-1.5 bg-green-600 text-white rounded text-xs font-bold hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors uppercase tracking-wide"
                title={status?.portConflict ? `Port ${config.port} is already in use` : ''}
              >
                {isLoading ? (statusMsg || 'STARTING') : 'START'}
              </button>
            )}
          </div>
        </div>

        {/* Collapsed View - Show only when collapsed */}
        {isCollapsed && (
          <div className="mt-2 pt-2 border-t border-gray-300 dark:border-gray-600 flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400 font-mono">
            {isRunning && status?.health && (
              <span title={`Service is ${status.health}`} className="text-lg">
                {getHealthIndicator(status.health)}
              </span>
            )}
            {config.port && (
              <span className="text-gray-600 dark:text-gray-300">:{config.port}</span>
            )}
            {status?.portConflict && (
              <span className="text-yellow-500 dark:text-yellow-400">⚠ CONFLICT</span>
            )}
            {build && build.status === 'building' && (
              <span className="text-blue-500 dark:text-blue-400">[BUILDING]</span>
            )}
            {build && build.status === 'failed' && (
              <span className="text-red-500 dark:text-red-400">[FAILED]</span>
            )}
            {serviceLogs.length > 0 && (
              <span className="text-gray-400 dark:text-gray-500">{serviceLogs.length} lines</span>
            )}
          </div>
        )}

        {/* Expanded View - Show only when not collapsed */}
        {!isCollapsed && (
          <>
            {/* Second Row: Status & Port | Environment Switcher */}
            <div className="flex items-center justify-between gap-4 flex-wrap mt-3">
          <div className="flex items-center gap-3 flex-wrap">
            {/* Status Badge */}
            <span
              className={`px-1.5 py-0.5 rounded text-[10px] font-bold uppercase ${
                isRunning
                  ? 'bg-green-600 text-white'
                  : isLoading && statusMsg
                  ? 'bg-yellow-600 text-white'
                  : 'bg-gray-600 text-white'
              }`}
            >
              {isRunning ? 'RUN' : isLoading && statusMsg ? statusMsg.toUpperCase().substring(0, 6) : 'STOP'}
            </span>
            {/* Health Indicator */}
            {isRunning && (
              <span className="text-lg" title={`Service is ${status?.health || 'unknown'}`}>
                {getHealthIndicator(status?.health)}
              </span>
            )}
            {/* Port Conflict Warning */}
            {status?.portConflict && (
              <span className="px-2 py-1 rounded text-sm bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300" title="Port conflict detected">
                ⚠ Port {config.port} in use
              </span>
            )}
            {/* Port Input */}
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
                  title={isRunning ? "Stop the service to change port" : "Edit port number"}
                />
              </div>
            )}
          </div>
          {/* Environment Switcher - Right Aligned */}
          <div className="flex-shrink-0">
            <EnvironmentSwitcher
              serviceName={config.name}
              currentEnv={currentEnv}
              envUrls={envUrls}
              environmentStatus={environmentStatus}
              isRunning={isRunning}
              onSwitch={onEnvironmentSwitch}
            />
          </div>
        </div>

        {/* URL and Deployment Info Row (for non-local environments) */}
        {currentEnv !== 'local' && config.url && (
          <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 space-y-1">
            <div className="flex items-center gap-2 text-sm">
              <span className="text-gray-600 dark:text-gray-400 font-medium">URL:</span>
              <code className="px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded text-xs text-gray-800 dark:text-gray-200 font-mono">
                {config.url}
              </code>
              <button
                onClick={() => navigator.clipboard.writeText(config.url || '')}
                className="px-2 py-1 text-xs bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-500 transition-colors"
                title="Copy URL to clipboard"
              >
                📋 Copy
              </button>
            </div>
            {deploymentInfo && (() => {
              const info = formatDeploymentInfo(deploymentInfo);
              return (
                <div className="flex items-center gap-2 text-xs">
                  <span className={`${info.statusColor}`} title={info.tooltip}>
                    {info.text}
                  </span>
                  {deploymentInfo.workflowRunUrl && (
                    <a
                      href={deploymentInfo.workflowRunUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-blue-500 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                      title="View deployment in GitHub Actions"
                    >
                      🔗 View Deployment
                    </a>
                  )}
                </div>
              );
            })()}
          </div>
        )}
        
        {/* Show ViewModeSelector during builds or when running */}
        {!isCollapsed && ((isBuilding || buildFailed) || (isRunning && config.url)) && (
          <ViewModeSelector
            config={config}
            currentMode={currentViewMode}
            isMaximized={isMaximized}
            onModeChange={onViewModeChange}
            onMaximize={onMaximize}
            onOpenInBrowser={onOpenInBrowser}
            onOpenInTauriWindow={onOpenInTauriWindow}
            onClearLogs={onClearLogs}
            hasLogs={serviceLogs.length > 0}
          />
        )}
          </>
        )}
      </div>
      
      {/* View Content: Logs, Webview, or Split - Show when expanded OR if there are important logs/builds */}
      {(!isCollapsed || hasImportantLogs) && showView && (
        <div className={`border-t border-gray-200 dark:border-gray-700 ${
          isMaximized 
            ? 'fixed inset-0 z-50 bg-white dark:bg-gray-900 flex flex-col' 
            : ''
        }`}>
          {isMaximized && (
            <div className="p-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between bg-gray-100 dark:bg-gray-800">
              <h3 className="font-semibold text-gray-900 dark:text-white">
                {config.displayName} - Maximized View
              </h3>
              <button
                onClick={onMaximize}
                className="px-3 py-1 bg-gray-500 text-white rounded text-sm hover:bg-gray-600"
              >
                Restore
              </button>
            </div>
          )}
          
          {isBuilding || buildFailed ? (
            <LogsViewer {...logsViewProps} />
          ) : currentViewMode === 'logs' ? (
            <LogsViewer {...logsViewProps} />
          ) : currentViewMode === 'webview' ? (
            isRunning && config.url ? (
              <WebviewView {...webviewViewProps} />
            ) : (
              <LogsViewer {...logsViewProps} />
            )
          ) : currentViewMode === 'split' ? (
            isRunning && config.url ? (
              <div className={`flex flex-1 ${isMaximized ? 'h-full min-h-0' : containerClass}`}>
                <div className="flex-1 border-r border-gray-200 dark:border-gray-700 min-w-0 flex flex-col">
                  <LogsViewer {...logsViewProps} />
                </div>
                <div className="flex-1 min-w-0 flex flex-col">
                  <WebviewView {...webviewViewProps} />
                </div>
              </div>
            ) : (
              <LogsViewer {...logsViewProps} />
            )
          ) : (
            <LogsViewer {...logsViewProps} />
          )}
        </div>
      )}
    </div>
  );
}

