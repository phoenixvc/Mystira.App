import { useState } from 'react';
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

  const toggleCollapse = () => {
    const newState = !isCollapsed;
    setIsCollapsed(newState);
    localStorage.setItem(`service-${config.name}-collapsed`, String(newState));
  };

  const isBuilding = build && build.status === 'building';
  const buildFailed = build && build.status === 'failed';
  const currentViewMode = (isBuilding || buildFailed) ? 'logs' : viewMode;
  const containerClass = isMaximized ? 'h-[calc(100vh-60px)]' : 'max-h-96';
  // Show view during builds, when running, or if there are any logs
  const showView = (build && (build.status === 'building' || build.status === 'failed')) || 
                  (isRunning && config.url) || 
                  (logs.length > 0) ||
                  isBuilding ||
                  buildFailed;

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
    <div className={`border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 shadow-sm transition-all ${
      currentEnv === 'prod' 
        ? 'border-l-4 border-red-500 bg-red-50/10 dark:bg-red-900/5' 
        : currentEnv === 'dev' 
        ? 'border-l-4 border-blue-500 bg-blue-50/10 dark:bg-blue-900/5' 
        : 'border-l-4 border-green-500'
    }`}>
      {/* Build Status Indicator */}
      {build && !isCollapsed && (
        <BuildStatusIndicator build={build} formatTimeSince={formatTimeSince} />
      )}
      
      <div className="p-4">
        {/* Top Row: Service Name + Environment Badge | Start/Stop Button */}
        <div className="flex items-center justify-between">
          <button
            onClick={toggleCollapse}
            className="flex items-center gap-2 flex-1 text-left hover:opacity-80 transition-opacity"
            title={isCollapsed ? 'Expand service details' : 'Collapse service details'}
          >
            <span className="text-gray-400 dark:text-gray-500 text-sm">
              {isCollapsed ? '▶' : '▼'}
            </span>
            <div className="flex items-center gap-2">
              <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
                {config.displayName}
              </h3>
            <span className={`px-2 py-0.5 rounded text-xs font-bold ${
              currentEnv === 'local' 
                ? 'bg-green-500 text-white' 
                : currentEnv === 'dev'
                ? 'bg-blue-500 text-white'
                : 'bg-red-600 text-white animate-pulse'
            }`}>
              {currentEnv === 'local' ? '🏠 LOCAL' : currentEnv === 'dev' ? '🧪 DEV' : '⚠️ PROD'}
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
                className={`px-2 py-1 rounded text-xs font-medium ${
                  isRunning
                    ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
                    : isLoading && statusMsg
                    ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300'
                    : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
                }`}
              >
                {isRunning ? 'Running' : isLoading && statusMsg ? statusMsg : 'Stopped'}
              </span>
            )}
            {isRunning ? (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onStop();
                }}
                disabled={isLoading}
                className="px-5 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed font-medium shadow-sm transition-colors"
              >
                {isLoading ? 'Stopping...' : 'Stop'}
              </button>
            ) : (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  onStart();
                }}
                disabled={isLoading || status?.portConflict}
                className="px-5 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed font-medium shadow-sm transition-colors"
                title={status?.portConflict ? `Port ${config.port} is already in use` : ''}
              >
                {isLoading ? (statusMsg || 'Starting...') : 'Start'}
              </button>
            )}
          </div>
        </div>

        {/* Collapsed View - Show only when collapsed */}
        {isCollapsed && (
          <div className="mt-2 pt-2 border-t border-gray-200 dark:border-gray-700 flex items-center gap-3 text-sm text-gray-600 dark:text-gray-400">
            {isRunning && status?.health && (
              <span title={`Service is ${status.health}`}>
                {getHealthIndicator(status.health)}
              </span>
            )}
            {config.port && (
              <span>Port: {config.port}</span>
            )}
            {status?.portConflict && (
              <span className="text-yellow-600 dark:text-yellow-400">⚠ Port conflict</span>
            )}
            {build && build.status === 'building' && (
              <span className="text-blue-600 dark:text-blue-400">🔨 Building...</span>
            )}
            {build && build.status === 'failed' && (
              <span className="text-red-600 dark:text-red-400">❌ Build failed</span>
            )}
            {serviceLogs.length > 0 && (
              <span>{serviceLogs.length} log{serviceLogs.length !== 1 ? 's' : ''}</span>
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
              className={`px-2 py-1 rounded text-sm font-medium ${
                isRunning
                  ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
                  : isLoading && statusMsg
                  ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
              }`}
            >
              {isRunning ? 'Running' : isLoading && statusMsg ? statusMsg : 'Stopped'}
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
              <div className="flex items-center gap-2">
                <span className="text-sm text-gray-600 dark:text-gray-400 font-medium">Port:</span>
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
      
      {/* View Content: Logs, Webview, or Split - Only show when expanded */}
      {!isCollapsed && showView && (
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

