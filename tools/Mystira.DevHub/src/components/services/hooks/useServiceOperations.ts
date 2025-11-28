import { invoke } from '@tauri-apps/api/tauri';
import type { ServiceConfig, ServiceStatus } from '../types';
import { getServiceConfigs } from '../index';

interface UseServiceOperationsProps {
  repoRoot: string;
  useCurrentBranch: boolean;
  currentBranch: string | null;
  customPorts: Record<string, number>;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string };
  services: ServiceStatus[];
  buildStatus: Record<string, { status: string; progress?: number } | undefined>;
  onRefreshServices: () => Promise<void>;
  onAddToast: (message: string, type: 'info' | 'success' | 'error' | 'warning', duration?: number) => void;
  prebuildService: (serviceName: string, repoRoot: string, onViewModeChange: (serviceName: string, mode: 'logs') => void, onShowLogs: (serviceName: string, show: boolean) => void, isManual?: boolean) => Promise<boolean>;
  viewMode: Record<string, 'logs' | 'webview' | 'split'>;
  setViewModeForService: (name: string, mode: 'logs' | 'webview' | 'split') => void;
  handleShowLogs: (name: string, show: boolean) => void;
  setServices: React.Dispatch<React.SetStateAction<ServiceStatus[]>>;
  setShowLogs: React.Dispatch<React.SetStateAction<Record<string, boolean>>>;
  setAutoScroll: React.Dispatch<React.SetStateAction<Record<string, boolean>>>;
}

export function useServiceOperations({
  repoRoot,
  useCurrentBranch,
  currentBranch,
  customPorts,
  serviceEnvironments,
  getEnvironmentUrls,
  services,
  buildStatus,
  onRefreshServices,
  onAddToast,
  prebuildService,
  viewMode,
  setViewModeForService,
  handleShowLogs,
  setServices,
  setShowLogs,
  setAutoScroll,
}: UseServiceOperationsProps) {
  const getRootToUse = () => {
    return useCurrentBranch && currentBranch 
      ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
      : repoRoot;
  };

  const startService = async (serviceName: string) => {
    const currentBuild = buildStatus[serviceName];
    if (currentBuild?.status === 'building') {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      onAddToast(`Cannot start ${config?.displayName || serviceName}: build is in progress. Please wait for the build to complete.`, 'warning', 5000);
      return;
    }

    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      const envName = environment.toUpperCase();
      onAddToast(`${serviceName} is set to ${envName} environment. It will connect to the deployed service, not start locally.`, 'info', 5000);
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
            onAddToast(`Port ${config.port} is already in use!`, 'warning', 7000);
            return;
          }
        } catch (portError) {
          console.warn('Port check failed, continuing anyway:', portError);
        }
      }
      
      const rootToUse = getRootToUse();
      await invoke<ServiceStatus>('start_service', { serviceName, repoRoot: rootToUse });
      await onRefreshServices();
      
      if (!viewMode[serviceName]) {
        setViewModeForService(serviceName, 'logs');
      }
      
      onAddToast(`${displayName} started successfully`, 'success');
    } catch (error: any) {
      const errorMessage = error?.message || String(error);
      if (errorMessage.includes('__TAURI_IPC__') || errorMessage.includes('not a function')) {
        onAddToast(`Tauri API error: Make sure you're running DevHub through Tauri (not in a browser). Restart the app if the issue persists.`, 'error', 10000);
      } else {
        onAddToast(`Failed to start ${serviceName}: ${errorMessage}`, 'error');
      }
    }
  };

  const stopService = async (serviceName: string) => {
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      setServices(prev => prev.map(s => s.name === serviceName ? { ...s, running: false } : s));
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      onAddToast(`${config?.displayName || serviceName} disconnected from ${environment.toUpperCase()} environment`, 'info');
      return;
    }
    
    try {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      await invoke('stop_service', { serviceName });
      await onRefreshServices();
      onAddToast(`${config?.displayName || serviceName} stopped`, 'info');
    } catch (error) {
      onAddToast(`Failed to stop ${serviceName}: ${error}`, 'error');
    }
  };

  const rebuildService = async (serviceName: string) => {
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      onAddToast(`Cannot rebuild ${serviceName}: it's connected to ${environment.toUpperCase()} environment`, 'info');
      return;
    }

    const rootToUse = getRootToUse();
    if (!rootToUse || rootToUse.trim() === '') {
      onAddToast('Repository root is not set. Please configure it first.', 'error');
      return;
    }

    const serviceStatus = services.find(s => s.name === serviceName);
    const wasRunning = serviceStatus?.running || false;
    
    if (wasRunning) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      onAddToast(`Stopping ${config?.displayName || serviceName} before rebuild...`, 'info', 2000);
      try {
        await stopService(serviceName);
        await new Promise(resolve => setTimeout(resolve, 1500));
        
        await onRefreshServices();
        const stillRunning = services.find(s => s.name === serviceName)?.running;
        if (stillRunning) {
          onAddToast(`Service ${serviceName} is still running. Please stop it manually and try again.`, 'error', 5000);
          return;
        }
      } catch (error) {
        onAddToast(`Failed to stop ${serviceName} before rebuild: ${error}`, 'error');
        return;
      }
    }

    try {
      const success = await prebuildService(serviceName, rootToUse, (name, mode) => setViewModeForService(name, mode), handleShowLogs, true);
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      if (success) {
        onAddToast(`${config?.displayName || serviceName} rebuilt successfully`, 'success');
      } else {
        onAddToast(`Failed to rebuild ${config?.displayName || serviceName}`, 'error');
      }
    } catch (error) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      onAddToast(`Failed to rebuild ${config?.displayName || serviceName}`, 'error');
    }
  };

  const startAllServices = async () => {
    const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const servicesToStart = serviceConfigs.filter((config: ServiceConfig) => {
      const status = services.find(s => s.name === config.name);
      const currentBuild = buildStatus[config.name];
      return !status?.running && currentBuild?.status !== 'building';
    });

    if (servicesToStart.length === 0) {
      const buildingServices = serviceConfigs.filter((config: ServiceConfig) => {
        const currentBuild = buildStatus[config.name];
        return currentBuild?.status === 'building';
      });
      
      if (buildingServices.length > 0) {
        onAddToast(`Cannot start services: ${buildingServices.map(s => s.displayName).join(', ')} ${buildingServices.length === 1 ? 'is' : 'are'} currently building. Please wait for the build to complete.`, 'warning', 5000);
      } else {
        onAddToast('All services are already running or configured for remote environments!', 'info');
      }
      return;
    }

    onAddToast(`Starting ${servicesToStart.length} service(s)... This may take a minute.`, 'info', 8000);
    
    try {
      const rootToUse = getRootToUse();
      for (let i = 0; i < servicesToStart.length; i++) {
        const service = servicesToStart[i];
        try {
          await invoke<ServiceStatus>('start_service', { serviceName: service.name, repoRoot: rootToUse });
          setShowLogs(prev => ({ ...prev, [service.name]: true }));
          setAutoScroll(prev => ({ ...prev, [service.name]: true }));
          onAddToast(`${service.displayName || service.name} started (${i + 1}/${servicesToStart.length})`, 'success', 3000);
        } catch (error) {
          console.error(`Failed to start ${service.name}:`, error);
          onAddToast(`Failed to start ${service.displayName || service.name}`, 'error');
        }
      }

      await onRefreshServices();
      onAddToast(`All ${servicesToStart.length} service(s) started successfully!`, 'success', 5000);
    } catch (error) {
      onAddToast(`Failed to start services: ${error}`, 'error');
    }
  };

  const stopAllServices = async () => {
    const runningServices = services.filter(s => s.running);
    if (runningServices.length === 0) {
      onAddToast('No services are running!', 'info');
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
      await onRefreshServices();
      onAddToast(`Stopped ${runningServices.length} service(s)`, 'info');
    } catch (error) {
      onAddToast(`Failed to stop services: ${error}`, 'error');
    }
  };

  const buildAllServices = async () => {
    const rootToUse = getRootToUse();
    if (!rootToUse || rootToUse.trim() === '') {
      onAddToast('Repository root is not set. Please configure it first.', 'error');
      return;
    }

    const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const servicesToBuild = serviceConfigs.filter((config: ServiceConfig) => {
      const environment = serviceEnvironments[config.name] || 'local';
      const currentBuild = buildStatus[config.name];
      return environment === 'local' && currentBuild?.status !== 'building';
    });

    if (servicesToBuild.length === 0) {
      const buildingServices = serviceConfigs.filter((config: ServiceConfig) => {
        const currentBuild = buildStatus[config.name];
        return currentBuild?.status === 'building';
      });

      if (buildingServices.length > 0) {
        onAddToast(
          `Cannot build: ${buildingServices.map(s => s.displayName).join(', ')} ${
            buildingServices.length === 1 ? 'is' : 'are'
          } currently building. Please wait for the build to complete.`,
          'warning',
          5000
        );
      } else {
        onAddToast('No local services to build. All services are configured for remote environments.', 'info');
      }
      return;
    }

    onAddToast(`Building ${servicesToBuild.length} service(s)... This may take a few minutes.`, 'info', 10000);

    let successCount = 0;
    let failCount = 0;

    for (const service of servicesToBuild) {
      try {
        const success = await prebuildService(service.name, rootToUse, (name, mode) => setViewModeForService(name, mode), handleShowLogs, true);
        if (success) {
          successCount++;
          onAddToast(`${service.displayName || service.name} built (${successCount}/${servicesToBuild.length})`, 'success', 3000);
        } else {
          failCount++;
          onAddToast(`Failed to build ${service.displayName || service.name}`, 'error');
        }
      } catch (error) {
        failCount++;
        console.error(`Failed to build ${service.name}:`, error);
        onAddToast(`Failed to build ${service.displayName || service.name}`, 'error');
      }
    }

    if (failCount === 0) {
      onAddToast(`All ${successCount} service(s) built successfully!`, 'success', 5000);
    } else {
      onAddToast(`Build complete: ${successCount} succeeded, ${failCount} failed`, failCount > 0 ? 'warning' : 'success', 5000);
    }
  };

  return {
    startService,
    stopService,
    rebuildService,
    startAllServices,
    stopAllServices,
    buildAllServices,
  };
}

