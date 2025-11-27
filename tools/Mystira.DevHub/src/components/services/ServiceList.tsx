import { useState } from 'react';
import { ServiceCard } from './ServiceCard';
import { BuildStatus, ServiceConfig, ServiceLog, ServiceStatus } from './types';
import { useServiceOrder } from './useServiceOrder';

interface ServiceListProps {
  serviceConfigs: ServiceConfig[];
  services: ServiceStatus[];
  buildStatus: Record<string, BuildStatus>;
  loading: Record<string, boolean>;
  statusMessage: Record<string, string>;
  logs: Record<string, ServiceLog[]>;
  getServiceLogs: (serviceName: string) => ServiceLog[];
  logFilters: Record<string, { search: string; type: 'all' | 'stdout' | 'stderr' }>;
  autoScroll: Record<string, boolean>;
  viewMode: Record<string, 'logs' | 'webview' | 'split'>;
  maximizedService: string | null;
  webviewErrors: Record<string, boolean>;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  environmentStatus: Record<string, { dev?: 'online' | 'offline' | 'checking'; prod?: 'online' | 'offline' | 'checking' }>;
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string };
  onStart: (serviceName: string) => void;
  onStop: (serviceName: string) => void;
  onRebuild?: (serviceName: string) => void;
  onPortChange: (serviceName: string, port: number) => void;
  onEnvironmentSwitch: (serviceName: string, env: 'local' | 'dev' | 'prod') => void;
  onViewModeChange: (serviceName: string, mode: 'logs' | 'webview' | 'split') => void;
  onMaximize: (serviceName: string) => void;
  onOpenInBrowser: (url: string) => void;
  onOpenInTauriWindow: (url: string, title: string) => void;
  onClearLogs: (serviceName: string) => void;
  onFilterChange: (serviceName: string, filter: { search: string; type: 'all' | 'stdout' | 'stderr' }) => void;
  onAutoScrollChange: (serviceName: string, enabled: boolean) => void;
  onWebviewRetry: (serviceName: string) => void;
  onWebviewError: (serviceName: string) => void;
  maxLogs?: number;
  onMaxLogsChange?: (limit: number) => void;
}

export function ServiceList({
  serviceConfigs,
  services,
  buildStatus,
  loading,
  statusMessage,
  logs,
  getServiceLogs,
  logFilters,
  autoScroll,
  viewMode,
  maximizedService,
  webviewErrors,
  serviceEnvironments,
  environmentStatus,
  getEnvironmentUrls,
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
}: ServiceListProps) {
  const { orderedServices, reorderServices } = useServiceOrder(serviceConfigs);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/html', index.toString());
    // Add visual feedback
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.style.opacity = '0.5';
    }
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (draggedIndex === null || draggedIndex === index) return;
    setDragOverIndex(index);
  };

  const handleDragLeave = () => {
    setDragOverIndex(null);
  };

  const handleDragEnd = (e: React.DragEvent) => {
    // Reset opacity
    if (e.currentTarget instanceof HTMLElement) {
      e.currentTarget.style.opacity = '1';
    }
    if (draggedIndex !== null && dragOverIndex !== null && draggedIndex !== dragOverIndex) {
      reorderServices(draggedIndex, dragOverIndex);
    }
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  const handleDrop = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    e.stopPropagation();
    if (draggedIndex !== null && draggedIndex !== index) {
      reorderServices(draggedIndex, index);
    }
    setDraggedIndex(null);
    setDragOverIndex(null);
  };

  return (
    <div className="space-y-2">
      {orderedServices.map((config, index) => {
        const status = services.find(s => s.name === config.name);
        const isRunning = status?.running || false;
        const isLoading = loading[config.name] || false;
        const statusMsg = statusMessage[config.name];
        const build = buildStatus[config.name];
        const serviceLogs = getServiceLogs(config.name);
        const filter = logFilters[config.name] || { search: '', type: 'all' };
        const isAutoScroll = autoScroll[config.name] !== false;

        return (
          <div
            key={config.name}
            draggable
            onDragStart={(e) => handleDragStart(e, index)}
            onDragOver={(e) => handleDragOver(e, index)}
            onDragLeave={handleDragLeave}
            onDragEnd={handleDragEnd}
            onDrop={(e) => handleDrop(e, index)}
            className={`transition-all cursor-move ${
              draggedIndex === index ? 'opacity-30' : ''
            } ${
              dragOverIndex === index && draggedIndex !== index 
                ? 'translate-y-1 border-t-2 border-blue-500' 
                : ''
            }`}
          >
            <ServiceCard
              config={config}
            status={status}
            build={build}
            isRunning={isRunning}
            isLoading={isLoading}
            statusMsg={statusMsg}
            serviceLogs={serviceLogs}
            logs={logs[config.name] || []}
            filter={filter}
            isAutoScroll={isAutoScroll}
            viewMode={viewMode[config.name] || 'logs'}
            isMaximized={maximizedService === config.name}
            webviewError={webviewErrors[config.name] || false}
            currentEnv={serviceEnvironments[config.name] || 'local'}
            envUrls={getEnvironmentUrls(config.name)}
            environmentStatus={environmentStatus[config.name]}
            deploymentInfo={null}
                onStart={() => onStart(config.name)}
                onStop={() => onStop(config.name)}
                onRebuild={onRebuild ? () => onRebuild(config.name) : undefined}
            onPortChange={(port) => onPortChange(config.name, port)}
            onEnvironmentSwitch={(env) => onEnvironmentSwitch(config.name, env)}
            onViewModeChange={(mode) => onViewModeChange(config.name, mode)}
            onMaximize={() => onMaximize(config.name)}
            onOpenInBrowser={onOpenInBrowser}
            onOpenInTauriWindow={onOpenInTauriWindow}
            onClearLogs={() => onClearLogs(config.name)}
            onFilterChange={(newFilter) => onFilterChange(config.name, newFilter)}
            onAutoScrollChange={(enabled) => onAutoScrollChange(config.name, enabled)}
            onWebviewRetry={() => onWebviewRetry(config.name)}
            onWebviewError={() => onWebviewError(config.name)}
            maxLogs={maxLogs}
            onMaxLogsChange={onMaxLogsChange}
          />
          </div>
        );
      })}
    </div>
  );
}

