import { LogsViewer } from '../LogsViewer';
import { WebviewView } from '../WebviewView';
import type { ServiceConfig, ServiceLog } from '../types';

interface ServiceCardViewContentProps {
  config: ServiceConfig;
  isBuilding: boolean;
  buildFailed: boolean;
  isRunning: boolean;
  viewMode: 'logs' | 'webview' | 'split';
  isMaximized: boolean;
  logs: ServiceLog[];
  filteredLogs: ServiceLog[];
  filter: { search: string; type: 'all' | 'stdout' | 'stderr' };
  isAutoScroll: boolean;
  maxLogs?: number;
  webviewError: boolean;
  containerClass: string;
  onFilterChange: (filter: { search: string; type: 'all' | 'stdout' | 'stderr' }) => void;
  onAutoScrollChange: (enabled: boolean) => void;
  onClearLogs: () => void;
  onMaxLogsChange?: (limit: number) => void;
  onWebviewRetry: () => void;
  onOpenInTauriWindow: (url: string, title: string) => void;
  onOpenInBrowser: (url: string) => void;
  onWebviewError: () => void;
}

export function ServiceCardViewContent({
  config,
  isBuilding,
  buildFailed,
  isRunning,
  viewMode,
  isMaximized,
  logs,
  filteredLogs,
  filter,
  isAutoScroll,
  maxLogs,
  webviewError,
  containerClass,
  onFilterChange,
  onAutoScrollChange,
  onClearLogs,
  onMaxLogsChange,
  onWebviewRetry,
  onOpenInTauriWindow,
  onOpenInBrowser,
  onWebviewError,
}: ServiceCardViewContentProps) {
  const logsViewProps = {
    serviceName: config.name,
    logs,
    filteredLogs,
    filter,
    isAutoScroll,
    isMaximized,
    containerClass,
    maxLogs,
    onFilterChange,
    onAutoScrollChange,
    onClearLogs,
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

  if (isBuilding || buildFailed) {
    return <LogsViewer {...logsViewProps} />;
  }

  if (viewMode === 'logs') {
    return <LogsViewer {...logsViewProps} />;
  }

  if (viewMode === 'webview') {
    return isRunning && config.url ? (
      <WebviewView {...webviewViewProps} />
    ) : (
      <LogsViewer {...logsViewProps} />
    );
  }

  if (viewMode === 'split') {
    return isRunning && config.url ? (
      <div className={`flex flex-1 min-h-0 ${isMaximized ? 'h-full' : ''}`}>
        <div className="flex-1 border-r border-gray-200 dark:border-gray-700 min-w-0 flex flex-col">
          <LogsViewer {...logsViewProps} />
        </div>
        <div className="flex-1 min-w-0 flex flex-col">
          <WebviewView {...webviewViewProps} />
        </div>
      </div>
    ) : (
      <LogsViewer {...logsViewProps} />
    );
  }

  return <LogsViewer {...logsViewProps} />;
}

