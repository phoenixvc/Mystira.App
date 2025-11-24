import { open } from '@tauri-apps/api/shell';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcut';
import { Toast, ToastContainer, useToast } from './Toast';
import {
  EnvironmentBanner,
  EnvironmentPresetSelector,
  RepositoryConfig,
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
  const { logs, logFilters, autoScroll, setLogFilters, setAutoScroll, getServiceLogs, clearLogs } = useServiceLogs();
  const { viewMode, maximizedService, webviewErrors, setViewModeForService, toggleMaximize, setWebviewErrors, setShowLogs } = useViewManagement();
  const { buildStatus, prebuildAllServices } = useBuildManagement();
  
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

  const getEnvironmentInfo = (serviceName: string) => {
    const env = serviceEnvironments[serviceName] || 'local';
    const config = serviceConfigs.find((c: ServiceConfig) => c.name === serviceName);
    const envUrls = getEnvironmentUrls(serviceName);
    return { environment: env, url: config?.url || '', hasDev: !!envUrls.dev, hasProd: !!envUrls.prod };
  };

  return (
    <div className="p-8">
      <ToastContainer toasts={toasts} onClose={removeToast} />
      
      <EnvironmentBanner
        serviceConfigs={serviceConfigs}
        serviceEnvironments={serviceEnvironments}
        environmentStatus={environmentStatus}
        getEnvironmentInfo={getEnvironmentInfo}
        onResetAll={() => {
          if (window.confirm('Switch all services to Local environment?\n\nThis will disconnect from deployed environments.')) {
            serviceConfigs.forEach((config: ServiceConfig) => {
              if (serviceEnvironments[config.name] && serviceEnvironments[config.name] !== 'local') {
                switchServiceEnvironment(config.name, 'local');
              }
            });
          }
        }}
      />
      
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Service Manager</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Keyboard shortcuts: Ctrl+Shift+S (Start All), Ctrl+Shift+X (Stop All), Ctrl+R (Refresh)
          </p>
        </div>
        <div className="flex gap-2 items-center">
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
              className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Start All
            </button>
          )}
          {anyRunning && (
            <button
              onClick={stopAllServices}
              className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Stop All
            </button>
          )}
        </div>
      </div>
      
      <RepositoryConfig
        repoRoot={repoRoot}
        currentBranch={currentBranch}
        useCurrentBranch={useCurrentBranch}
        onRepoRootChange={setRepoRoot}
        onUseCurrentBranchChange={setUseCurrentBranch}
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
      />
    </div>
  );
}

export default ServiceManager;

