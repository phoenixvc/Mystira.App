import { open } from '@tauri-apps/api/shell';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcut';
import {
  ServiceList,
  ServiceManagerHeader,
  getServiceConfigs,
  useBuildManagement,
  useEnvironmentManagement,
  useInfrastructureStatus,
  usePortManagement,
  useRepositoryConfig,
  useServiceEnvironment,
  useServiceLogs,
  useServiceOperations,
  useServiceRefresh,
  useViewManagement,
  type ServiceConfig,
  type ServiceStatus,
} from './services';
import { ToastContainer, useToast, type Toast } from './ui';

function ServiceManager() {
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    const saved = localStorage.getItem('serviceEnvironments');
    return saved ? JSON.parse(saved) : {};
  });
  const { toasts, showToast, dismissToast } = useToast();

  const addToast = (message: string, type: Toast['type'] = 'info', duration: number = 5000) => {
    showToast(message, type, { duration });
  };

  const { repoRoot, currentBranch, useCurrentBranch, setRepoRoot, setUseCurrentBranch } = useRepositoryConfig();
  const { environmentStatus, getEnvironmentUrls, fetchEnvironmentUrls, checkEnvironmentHealth } = useEnvironmentManagement();
  const { customPorts, updateServicePort, loadPortsFromFiles } = usePortManagement(
    repoRoot,
    serviceEnvironments,
    getEnvironmentUrls,
    addToast
  );
  const { logs, logFilters, autoScroll, maxLogs, setLogFilters, setAutoScroll, getServiceLogs, clearLogs, updateMaxLogs } = useServiceLogs();
  const { viewMode, maximizedService, webviewErrors, setViewModeForService, toggleMaximize, setWebviewErrors, setShowLogs } = useViewManagement();
  const { buildStatus, prebuildService, prebuildAllServices } = useBuildManagement();
  const { services, setServices, refreshServices } = useServiceRefresh();
  const { infrastructureStatus, checkInfrastructureStatus } = useInfrastructureStatus({ serviceEnvironments });
  
  const handleShowLogs = (serviceName: string, show: boolean) => {
    setShowLogs(prev => ({ ...prev, [serviceName]: show }));
  };

  const prebuildServiceWrapper = async (
    serviceName: string,
    repoRoot: string,
    onViewModeChange: (serviceName: string, mode: 'logs') => void,
    onShowLogs: (serviceName: string, show: boolean) => void,
    isManual: boolean = false
  ): Promise<boolean> => {
    return prebuildService(serviceName, repoRoot, onViewModeChange, onShowLogs, isManual);
  };

  const {
    startService,
    stopService,
    rebuildService,
    startAllServices,
    stopAllServices,
    buildAllServices,
  } = useServiceOperations({
    repoRoot,
    useCurrentBranch,
    currentBranch,
    customPorts,
    serviceEnvironments,
    getEnvironmentUrls,
    services,
    buildStatus,
    onRefreshServices: refreshServices,
    onAddToast: addToast,
    prebuildService: prebuildServiceWrapper,
    viewMode,
    setViewModeForService,
    handleShowLogs,
    setServices,
    setShowLogs,
    setAutoScroll,
  });

  const { switchServiceEnvironment } = useServiceEnvironment({
    customPorts,
      serviceEnvironments,
    getEnvironmentUrls,
    services,
    onStopService: stopService,
    onServiceEnvironmentsChange: setServiceEnvironments,
    onCheckEnvironmentHealth: checkEnvironmentHealth,
    onAddToast: addToast,
  });

  const openInBrowser = async (url: string) => {
    try {
      await open(url);
    } catch (error) {
      console.error('Failed to open URL:', error);
      addToast('Failed to open URL in browser', 'error');
    }
  };

  const openInTauriWindow = async (url: string, title: string) => {
    try {
      await invoke('create_webview_window', { url, title });
      addToast(`Opened ${title} in Tauri window`, 'success');
    } catch (error) {
      console.error('Failed to create Tauri window:', error);
      addToast('Failed to open Tauri window, opening in external browser instead', 'warning');
      await openInBrowser(url);
    }
  };

  useEffect(() => {
    const initialize = async () => {
      try {
        await loadPortsFromFiles(repoRoot);
        await refreshServices();
        
        checkInfrastructureStatus('dev');
        checkInfrastructureStatus('prod');
        
        setTimeout(() => {
          prebuildAllServices(
            repoRoot,
            getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls),
            useCurrentBranch,
            currentBranch,
            setViewModeForService,
            handleShowLogs
          );
        }, 1000);
        
        fetchEnvironmentUrls().then(() => {
          setTimeout(() => {
            getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).forEach((config: ServiceConfig) => {
              const envUrls = getEnvironmentUrls(config.name);
              if (envUrls.dev) checkEnvironmentHealth(config.name, 'dev');
              if (envUrls.prod) checkEnvironmentHealth(config.name, 'prod');
            });
          }, 1000);
        });
      } catch (error) {
        console.error('Failed to initialize:', error);
        addToast('Warning: Not running in Tauri. Please use the Tauri application window.', 'warning', 5000);
      }
    };
    
    initialize();
    const interval = setInterval(refreshServices, 2000);
    const healthCheckInterval = setInterval(() => {
      ['api', 'admin-api', 'pwa'].forEach(name => {
        const envUrls = getEnvironmentUrls(name);
        if (envUrls.dev) checkEnvironmentHealth(name, 'dev');
        if (envUrls.prod) checkEnvironmentHealth(name, 'prod');
      });
    }, 30000);
    
    return () => {
      clearInterval(interval);
      clearInterval(healthCheckInterval);
    };
  }, []);

  useKeyboardShortcuts([
    { key: 'b', ctrl: true, shift: true, action: buildAllServices, description: 'Build all services' },
    { key: 's', ctrl: true, shift: true, action: startAllServices, description: 'Start all services' },
    { key: 'x', ctrl: true, shift: true, action: stopAllServices, description: 'Stop all services' },
    { key: 'r', ctrl: true, action: refreshServices, description: 'Refresh services' },
  ]);

  const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
  const allRunning = services.length === serviceConfigs.length && services.every((s: ServiceStatus) => s.running);
  const anyRunning = services.some((s: ServiceStatus) => s.running);
  const anyBuilding = Object.values(buildStatus).some((status: any) => status?.status === 'building');

  const handleApplyPreset = (preset: any) => {
    addToast(`Applied preset: ${preset.name}`, 'success');
  };

  return (
    <div className="p-8">
      <ToastContainer toasts={toasts} onClose={dismissToast} />
      
      <ServiceManagerHeader
        repoRoot={repoRoot}
        currentBranch={currentBranch}
        useCurrentBranch={useCurrentBranch}
        onRepoRootChange={setRepoRoot}
        onUseCurrentBranchChange={setUseCurrentBranch}
        serviceEnvironments={serviceEnvironments}
        onServiceEnvironmentsChange={setServiceEnvironments}
        infrastructureStatus={infrastructureStatus}
        onApplyPreset={handleApplyPreset}
        onBuildAll={buildAllServices}
        onStartAll={startAllServices}
        onStopAll={stopAllServices}
        anyBuilding={anyBuilding}
        allRunning={allRunning}
        anyRunning={anyRunning}
        onCheckEnvironmentHealth={checkEnvironmentHealth}
      />
      
      <ServiceList
        serviceConfigs={serviceConfigs}
        services={services}
        buildStatus={buildStatus}
        loading={{}}
        statusMessage={{}}
        logs={logs}
        getServiceLogs={getServiceLogs}
        logFilters={logFilters}
        autoScroll={autoScroll}
        viewMode={viewMode}
        maximizedService={maximizedService}
        webviewErrors={webviewErrors}
        serviceEnvironments={serviceEnvironments}
        environmentStatus={environmentStatus}
        getEnvironmentUrls={getEnvironmentUrls}
        onStart={startService}
        onStop={stopService}
        onRebuild={rebuildService}
        onPortChange={(name, port) => updateServicePort(name, port, false)}
        onEnvironmentSwitch={switchServiceEnvironment}
        onViewModeChange={setViewModeForService}
        onMaximize={toggleMaximize}
        onOpenInBrowser={openInBrowser}
        onOpenInTauriWindow={openInTauriWindow}
        onClearLogs={clearLogs}
        onFilterChange={(name, filter) => setLogFilters(prev => ({ ...prev, [name]: filter }))}
        onAutoScrollChange={(name, enabled) => setAutoScroll(prev => ({ ...prev, [name]: enabled }))}
        onWebviewRetry={(name) => setWebviewErrors(prev => ({ ...prev, [name]: false }))}
        onWebviewError={(name) => setWebviewErrors(prev => ({ ...prev, [name]: true }))}
        maxLogs={maxLogs}
        onMaxLogsChange={updateMaxLogs}
      />
    </div>
  );
}

export default ServiceManager;
