import { open } from '@tauri-apps/api/shell';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcut';
import { Toast, ToastContainer, useToast } from './Toast';
import {
  EnvironmentPresetSelector,
  ServiceList,
  checkEnvironmentContext,
  getServiceConfigs,
  useBuildManagement,
  useEnvironmentManagement,
  usePortManagement,
  useRepositoryConfig,
  useServiceLogs,
  useViewManagement,
  type EnvironmentPreset,
  type ServiceConfig,
  type ServiceStatus,
} from './services';

function ServiceManager() {
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    const saved = localStorage.getItem('serviceEnvironments');
    return saved ? JSON.parse(saved) : {};
  });
  const [services, setServices] = useState<ServiceStatus[]>([]);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const { showToast } = useToast();

  const addToast = (message: string, type: Toast['type'] = 'info', duration: number = 5000) => {
    const toast = showToast(message, type, duration);
    setToasts((prev) => [...prev, toast]);
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
  
  const handleShowLogs = (serviceName: string, show: boolean) => {
    setShowLogs(prev => ({ ...prev, [serviceName]: show }));
  };

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const refreshServices = async () => {
    try {
      const statuses = await invoke<ServiceStatus[]>('get_service_status');
      const enrichedStatuses = await Promise.all(
        statuses.map(async (status) => {
          let portConflict = false;
          let health: 'healthy' | 'unhealthy' | 'unknown' = 'unknown';
          
          if (status.port) {
            try {
              const available = await invoke<boolean>('check_port_available', { port: status.port });
              portConflict = !available && !status.running;
            } catch (error) {
              console.error(`Failed to check port ${status.port}:`, error);
            }
          }
          
          if (status.running && status.url) {
            try {
              const isHealthy = await invoke<boolean>('check_service_health', { url: status.url });
              health = isHealthy ? 'healthy' : 'unhealthy';
            } catch (error) {
              console.error(`Failed to check health for ${status.name}:`, error);
            }
          }
          
          return { ...status, portConflict, health };
        })
      );
      
      setServices(prev => {
        const hasChanged = enrichedStatuses.length !== prev.length ||
          enrichedStatuses.some(newStatus => {
            const oldStatus = prev.find(s => s.name === newStatus.name);
            if (!oldStatus) return true;
            return oldStatus.running !== newStatus.running ||
                   oldStatus.port !== newStatus.port ||
                   oldStatus.url !== newStatus.url ||
                   oldStatus.health !== newStatus.health ||
                   oldStatus.portConflict !== newStatus.portConflict;
          });
        return hasChanged ? enrichedStatuses : prev;
      });
    } catch (error) {
      console.error('Failed to get service status:', error);
    }
  };

  const startService = async (serviceName: string) => {
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      const envName = environment.toUpperCase();
      addToast(`${serviceName} is set to ${envName} environment. It will connect to the deployed service, not start locally.`, 'info', 5000);
      setServices(prev => {
        const existing = prev.find(s => s.name === serviceName);
        if (existing) {
          return prev.map(s => s.name === serviceName ? { ...s, running: true } : s);
        }
        const envUrls = getEnvironmentUrls(serviceName);
        const url = environment === 'dev' ? envUrls.dev : envUrls.prod;
        return [...prev, { name: serviceName, running: true, url }];
      });
      return;
    }
    
    try {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      const displayName = config?.displayName || serviceName;
      
      if (config?.port) {
        try {
          const available = await invoke<boolean>('check_port_available', { port: config.port });
          if (!available) {
            addToast(`Port ${config.port} is already in use!`, 'warning', 7000);
            return;
          }
        } catch (portError) {
          console.warn('Port check failed, continuing anyway:', portError);
        }
      }
      
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;
      
      await invoke<ServiceStatus>('start_service', { serviceName, repoRoot: rootToUse });
      await refreshServices();
      
      if (!viewMode[serviceName]) {
        setViewModeForService(serviceName, 'logs');
      }
      
      addToast(`${displayName} started successfully`, 'success');
    } catch (error: any) {
      const errorMessage = error?.message || String(error);
      if (errorMessage.includes('__TAURI_IPC__') || errorMessage.includes('not a function')) {
        addToast(`Tauri API error: Make sure you're running DevHub through Tauri (not in a browser). Restart the app if the issue persists.`, 'error', 10000);
      } else {
        addToast(`Failed to start ${serviceName}: ${errorMessage}`, 'error');
      }
    }
  };

  const stopService = async (serviceName: string) => {
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      setServices(prev => prev.map(s => s.name === serviceName ? { ...s, running: false } : s));
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      addToast(`${config?.displayName || serviceName} disconnected from ${environment.toUpperCase()} environment`, 'info');
      return;
    }
    
    try {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      await invoke('stop_service', { serviceName });
      await refreshServices();
      addToast(`${config?.displayName || serviceName} stopped`, 'info');
    } catch (error) {
      addToast(`Failed to stop ${serviceName}: ${error}`, 'error');
    }
  };

  const rebuildService = async (serviceName: string) => {
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      addToast(`Cannot rebuild ${serviceName}: it's connected to ${environment.toUpperCase()} environment`, 'info');
      return;
    }

    const rootToUse = useCurrentBranch && currentBranch 
      ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
      : repoRoot;

    if (!rootToUse || rootToUse.trim() === '') {
      addToast('Repository root is not set. Please configure it first.', 'error');
      return;
    }

    try {
      await prebuildService(serviceName, rootToUse, setViewModeForService, handleShowLogs, true);
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      addToast(`${config?.displayName || serviceName} rebuilt successfully`, 'success');
    } catch (error) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      addToast(`Failed to rebuild ${config?.displayName || serviceName}`, 'error');
    }
  };

  const startAllServices = async () => {
    const servicesToStart = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).filter((config: ServiceConfig) => {
      const status = services.find(s => s.name === config.name);
      return !status?.running;
    });

    if (servicesToStart.length === 0) {
      addToast('All services are already running!', 'info');
      return;
    }

    addToast(`Starting ${servicesToStart.length} service(s)... This may take a minute.`, 'info', 8000);
    
    try {
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;

      for (let i = 0; i < servicesToStart.length; i++) {
        const service = servicesToStart[i];
        try {
          await invoke<ServiceStatus>('start_service', { serviceName: service.name, repoRoot: rootToUse });
          setShowLogs(prev => ({ ...prev, [service.name]: true }));
          setAutoScroll(prev => ({ ...prev, [service.name]: true }));
          addToast(`${service.displayName || service.name} started (${i + 1}/${servicesToStart.length})`, 'success', 3000);
        } catch (error) {
          console.error(`Failed to start ${service.name}:`, error);
          addToast(`Failed to start ${service.displayName || service.name}`, 'error');
        }
      }

      await refreshServices();
      addToast(`All ${servicesToStart.length} service(s) started successfully!`, 'success', 5000);
    } catch (error) {
      addToast(`Failed to start services: ${error}`, 'error');
    }
  };

  const stopAllServices = async () => {
    const runningServices = services.filter(s => s.running);
    
    if (runningServices.length === 0) {
      addToast('No services are running!', 'info');
      return;
    }

    try {
      const stopPromises = runningServices.map(service => 
        invoke('stop_service', { serviceName: service.name }).catch(error => {
          console.error(`Failed to stop ${service.name}:`, error);
          return { service: service.name, error };
        })
      );

      await Promise.allSettled(stopPromises);
      await refreshServices();
      addToast(`Stopped ${runningServices.length} service(s)`, 'info');
    } catch (error) {
      addToast(`Failed to stop services: ${error}`, 'error');
    }
  };

  const switchServiceEnvironment = async (serviceName: string, environment: 'local' | 'dev' | 'prod') => {
    const contextCheck = checkEnvironmentContext(
      serviceName,
      environment,
      serviceEnvironments,
      getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls)
    );
    
    if (contextCheck.shouldWarn) {
      const confirmed = window.confirm(contextCheck.message);
      if (!confirmed) return;
    }
    
    if (environment === 'prod') {
      const confirmed = window.confirm(
        '⚠️ DANGER: PRODUCTION ENVIRONMENT ⚠️\n\n' +
        'You are about to switch to the PRODUCTION environment.\n\n' +
        'This will connect to live production services and could:\n' +
        '• Affect real user data\n' +
        '• Cause unintended side effects\n' +
        '• Impact production systems\n\n' +
        'Are you absolutely sure you want to continue?\n\n' +
        'Click OK to proceed, or Cancel to abort.'
      );
      if (!confirmed) return;
    }
    
    const status = services.find(s => s.name === serviceName);
    if (status?.running) {
      const stopConfirmed = window.confirm(
        `The ${serviceName} service is currently running. It needs to be stopped before switching environments.\n\nWould you like to stop it now?`
      );
      if (stopConfirmed) {
        await stopService(serviceName);
      } else {
        return;
      }
    }
    
    setServiceEnvironments(prev => {
      const updated = { ...prev, [serviceName]: environment };
      localStorage.setItem('serviceEnvironments', JSON.stringify(updated));
      return updated;
    });
    
    if (environment !== 'local') {
      checkEnvironmentHealth(serviceName, environment);
    }
    
    const envName = environment === 'local' ? 'Local' : environment.toUpperCase();
    addToast(`${serviceName} switched to ${envName} environment`, 'success');
  };

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
    { key: 's', ctrl: true, shift: true, action: startAllServices, description: 'Start all services' },
    { key: 'x', ctrl: true, shift: true, action: stopAllServices, description: 'Stop all services' },
    { key: 'r', ctrl: true, action: refreshServices, description: 'Refresh services' },
  ]);

  const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
  const allRunning = services.length === serviceConfigs.length && services.every((s: ServiceStatus) => s.running);
  const anyRunning = services.some((s: ServiceStatus) => s.running);

  return (
    <div className="p-8">
      <ToastContainer toasts={toasts} onClose={removeToast} />
      
      {/* Combined Title and Repository Config */}
      <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div className="flex items-center gap-4 flex-1 min-w-0">
            <h1 className="text-xl font-bold text-gray-900 dark:text-white font-mono">SERVICE MANAGER</h1>
            <span className="text-xs text-gray-500 dark:text-gray-400 hidden sm:inline">
              Ctrl+Shift+S (Start All) | Ctrl+Shift+X (Stop All) | Ctrl+R (Refresh)
            </span>
          </div>
          <div className="flex gap-2 items-center flex-shrink-0">
            <EnvironmentPresetSelector
              currentEnvironments={serviceEnvironments}
              onApplyPreset={(preset: EnvironmentPreset) => {
                const hasProd = Object.values(preset.environments).includes('prod');
                if (hasProd) {
                  const confirmed = window.confirm('⚠️ WARNING: This preset includes PRODUCTION environments.\n\nAre you sure you want to apply this preset?');
                  if (!confirmed) return;
                }
                
                setServiceEnvironments(preset.environments);
                localStorage.setItem('serviceEnvironments', JSON.stringify(preset.environments));
                
                Object.entries(preset.environments).forEach(([serviceName, env]) => {
                  if (env !== 'local' && (env === 'dev' || env === 'prod')) {
                    checkEnvironmentHealth(serviceName, env as 'dev' | 'prod');
                  }
                });
                
                addToast(`Applied preset: ${preset.name}`, 'success');
              }}
              onSaveCurrent={() => {}}
            />
            {!allRunning && (
              <button
                onClick={startAllServices}
                disabled={anyRunning}
                className="px-3 py-1.5 bg-green-600 text-white rounded text-sm hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
              >
                Start All
              </button>
            )}
            {anyRunning && (
              <button
                onClick={stopAllServices}
                className="px-3 py-1.5 bg-red-600 text-white rounded text-sm hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
              >
                Stop All
              </button>
            )}
          </div>
        </div>
        <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 flex items-center gap-3 flex-wrap">
          <div className="flex items-center gap-2 flex-1 min-w-0">
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 whitespace-nowrap">Repo:</label>
            <input
              type="text"
              value={repoRoot}
              onChange={(e) => setRepoRoot(e.target.value)}
              className="flex-1 min-w-0 px-2 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
              placeholder="C:\Users\smitj\repos\Mystira.App"
            />
            <button
              onClick={async () => {
                try {
                  const { open } = await import('@tauri-apps/api/dialog');
                  const selected = await open({
                    directory: true,
                    multiple: false,
                    defaultPath: repoRoot || undefined,
                  });
                  
                  if (selected && typeof selected === 'string') {
                    setRepoRoot(selected);
                    try {
                      await invoke<string>('get_current_branch', { repoRoot: selected });
                    } catch (error) {
                      console.warn('Failed to get current branch:', error);
                    }
                  }
                } catch (error) {
                  console.error('Failed to pick repo root:', error);
                }
              }}
              className="px-2 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 flex-shrink-0"
            >
              Browse
            </button>
          </div>
          {currentBranch && (
            <div className="flex items-center gap-2 flex-shrink-0">
              <span className="text-xs text-gray-600 dark:text-gray-400">Branch:</span>
              <span className="px-2 py-0.5 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded font-mono">{currentBranch}</span>
              <label className="flex items-center gap-1 text-xs text-gray-600 dark:text-gray-400">
                <input
                  type="checkbox"
                  checked={useCurrentBranch}
                  onChange={(e) => setUseCurrentBranch(e.target.checked)}
                  className="w-3 h-3"
                />
                <span>Use branch dir</span>
              </label>
            </div>
          )}
        </div>
      </div>
      
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

