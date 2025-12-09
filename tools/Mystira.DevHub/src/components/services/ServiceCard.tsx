import { useEffect, useRef, useState } from 'react';
import { BuildStatusIndicator } from './components';
import type { DeploymentInfo } from './components';
import { EnvironmentSwitcher } from './environment';
import { ViewModeSelector } from './components';
import {
    ServiceCardControls,
    ServiceCardDeploymentInfo,
    ServiceCardHeader,
    ServiceCardStatusRow,
    ServiceCardViewContent,
    useServiceCardResize,
} from './card';
import type { BuildStatus, ServiceConfig, ServiceLog, ServiceStatus } from './types';
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
  environmentStatus?: {
    dev?: 'online' | 'offline' | 'checking';
    prod?: 'online' | 'offline' | 'checking';
  };
  deploymentInfo?: DeploymentInfo | null;
  onStart: () => void;
  onStop: () => void;
  onRebuild?: () => void;
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
  onRebuild,
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
  const scrollPositionRef = useRef<number>(0);
  const logContainerRef = useRef<HTMLDivElement | null>(null);
  const { logHeight, resizeHandleRef, handleResizeStart } = useServiceCardResize(config.name);

  const isBuilding = !!(build && build.status === 'building');
  const buildFailed = !!(build && build.status === 'failed');
  const hasImportantLogs = logs.length > 0 || isBuilding || buildFailed;

  const errorCount = serviceLogs.filter((log) => {
    const msg = log.message.toLowerCase();
    return (
      log.type === 'stderr' ||
      msg.includes('error') ||
      msg.includes('failed') ||
      msg.includes('exception') ||
      msg.includes('fatal')
    );
  }).length;

  const warningCount = serviceLogs.filter((log) => {
    const msg = log.message.toLowerCase();
    return msg.includes('warning') || msg.includes('warn') || msg.includes('deprecated');
  }).length;

  useEffect(() => {
    if ((isBuilding || buildFailed) && !isCollapsed) {
      onViewModeChange('logs');
    }
  }, [isBuilding, buildFailed, onViewModeChange, isCollapsed]);

  const toggleCollapse = () => {
    const newState = !isCollapsed;
    if (newState && !isCollapsed) {
      const logContainer = logContainerRef.current?.querySelector('.overflow-y-auto') as HTMLElement;
      if (logContainer) {
        scrollPositionRef.current = logContainer.scrollTop;
      }
    }
    setIsCollapsed(newState);
    localStorage.setItem(`service-${config.name}-collapsed`, String(newState));
    if (!newState && isCollapsed && scrollPositionRef.current > 0) {
      setTimeout(() => {
        const logContainer = logContainerRef.current?.querySelector('.overflow-y-auto') as HTMLElement;
        if (logContainer) {
          logContainer.scrollTop = scrollPositionRef.current;
        }
      }, 100);
    }
  };

  const currentViewMode = isBuilding || buildFailed ? 'logs' : viewMode;
  const containerClass = isMaximized ? 'h-[calc(100vh-60px)]' : 'flex-1 min-h-0';
  const showView = hasImportantLogs || (isRunning && config.url) || isBuilding || buildFailed;

  return (
    <div
      className={`border border-gray-300 dark:border-gray-600 rounded-md bg-gray-50 dark:bg-gray-900 shadow-sm transition-all font-mono relative ${
        currentEnv === 'prod'
          ? 'border-l-4 border-red-500 bg-red-950/20 dark:bg-red-950/30'
          : currentEnv === 'dev'
          ? 'border-l-4 border-blue-500 bg-blue-950/20 dark:bg-blue-950/30'
          : 'border-l-4 border-green-500 bg-green-950/10 dark:bg-green-950/20'
      }`}
    >
      {build && (build.status === 'building' || build.status === 'failed' || build.lastBuildTime) && (
        <BuildStatusIndicator build={build} formatTimeSince={formatTimeSince} />
      )}

      <div className="absolute left-0 top-0 bottom-0 w-1 bg-gray-400 dark:bg-gray-600 hover:bg-blue-500 dark:hover:bg-blue-400 cursor-move opacity-0 hover:opacity-100 transition-opacity" title="Drag to reorder" />

      <div className="p-3">
        <div className="flex items-center justify-between gap-2">
          <ServiceCardHeader
            config={config}
            isCollapsed={isCollapsed}
            isBuilding={isBuilding}
            buildFailed={buildFailed}
            logsCount={logs.length}
            currentEnv={currentEnv}
            environmentStatus={environmentStatus}
            onToggleCollapse={toggleCollapse}
          />
          <ServiceCardControls
            isRunning={isRunning}
            isLoading={isLoading}
            isBuilding={isBuilding}
            statusMsg={statusMsg}
            onStart={onStart}
            onStop={onStop}
            onRebuild={onRebuild}
          />
        </div>

        {isCollapsed && (
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
            {(isBuilding || buildFailed || logs.length > 0) && (
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
                  : logs.length > 0
                  ? `${logs.length} logs`
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
        )}

        {!isCollapsed && (
          <>
            <ServiceCardStatusRow
              config={config}
              status={status}
              build={build}
              isRunning={isRunning}
              isLoading={isLoading}
              statusMsg={statusMsg}
              errorCount={errorCount}
              warningCount={warningCount}
              onPortChange={onPortChange}
            />
            <div className="flex-shrink-0 mt-3">
              <EnvironmentSwitcher
                serviceName={config.name}
                currentEnv={currentEnv}
                envUrls={envUrls}
                environmentStatus={environmentStatus}
                isRunning={isRunning}
                onSwitch={onEnvironmentSwitch}
              />
            </div>
            <ServiceCardDeploymentInfo config={config} currentEnv={currentEnv} deploymentInfo={deploymentInfo} />
            {(isBuilding || buildFailed || (isRunning && config.url)) && (
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

      {!isCollapsed && showView && (
        <div
          ref={logContainerRef}
          className={`border-t border-gray-200 dark:border-gray-700 relative flex flex-col ${
            isMaximized ? 'fixed inset-0 z-50 bg-white dark:bg-gray-900' : 'overflow-hidden'
          }`}
          style={!isMaximized ? { height: `${logHeight}px` } : undefined}
        >
          {isMaximized && (
            <div className="p-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between bg-gray-100 dark:bg-gray-800">
              <h3 className="font-semibold text-gray-900 dark:text-white">{config.displayName} - Maximized View</h3>
              <button onClick={onMaximize} className="px-3 py-1 bg-gray-500 text-white rounded text-sm hover:bg-gray-600">
                Restore
              </button>
            </div>
          )}
          <ServiceCardViewContent
            config={config}
            isBuilding={isBuilding}
            buildFailed={buildFailed}
            isRunning={isRunning}
            viewMode={currentViewMode}
            isMaximized={isMaximized}
            logs={logs}
            filteredLogs={serviceLogs}
            filter={filter}
            isAutoScroll={isAutoScroll}
            maxLogs={maxLogs}
            webviewError={webviewError}
            containerClass={containerClass}
            onFilterChange={onFilterChange}
            onAutoScrollChange={onAutoScrollChange}
            onClearLogs={onClearLogs}
            onMaxLogsChange={onMaxLogsChange}
            onWebviewRetry={onWebviewRetry}
            onOpenInTauriWindow={onOpenInTauriWindow}
            onOpenInBrowser={onOpenInBrowser}
            onWebviewError={onWebviewError}
          />
          {!isMaximized && (
            <div
              ref={resizeHandleRef}
              onMouseDown={handleResizeStart}
              className="absolute bottom-0 left-0 right-0 h-2 cursor-ns-resize hover:bg-blue-500/20 dark:hover:bg-blue-400/20 transition-colors group z-20 pointer-events-auto"
              title="Drag to resize log window"
            >
              <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-12 h-0.5 bg-gray-400 dark:bg-gray-500 group-hover:bg-blue-500 dark:group-hover:bg-blue-400 rounded transition-colors pointer-events-none" />
            </div>
          )}
        </div>
      )}
    </div>
  );
}


