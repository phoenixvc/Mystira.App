import { invoke } from '@tauri-apps/api/tauri';
import type { ServiceStatus } from '../../services/types';

interface UseServiceLifecycleProps {
  repoRoot: string;
  useCurrentBranch: boolean;
  currentBranch: string | null;
  customPorts: Record<string, number>;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string };
  getServiceConfigs: (
    customPorts: Record<string, number>,
    serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>,
    getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string }
  ) => any[];
  services: ServiceStatus[];
  buildStatus: Record<string, any>;
  prebuildService: (
    serviceName: string,
    repoRoot: string,
    setViewModeForService: (serviceName: string, mode: 'logs' | 'webview' | 'split') => void,
    handleShowLogs: (serviceName: string, show: boolean) => void,
    isManual: boolean
  ) => Promise<boolean>;
  setViewModeForService: (serviceName: string, mode: 'logs' | 'webview' | 'split') => void;
  handleShowLogs: (serviceName: string, show: boolean) => void;
  setAutoScroll: React.Dispatch<React.SetStateAction<Record<string, boolean>>>;
  addToast: (message: string, type?: 'info' | 'success' | 'error' | 'warning', duration?: number) => void;
  refreshServices: () => Promise<void>;
}

export function useServiceLifecycle({
  repoRoot,
  useCurrentBranch,
  currentBranch,
  customPorts,
  serviceEnvironments,
  getEnvironmentUrls,
  getServiceConfigs,
  services,
  buildStatus,
  prebuildService,
  setViewModeForService,
  handleShowLogs,
  setAutoScroll,
  addToast,
  refreshServices,
}: UseServiceLifecycleProps) {
  const getRootToUse = () => {
    return useCurrentBranch && currentBranch
      ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
      : repoRoot;
  };

  const startService = async (serviceName: string) => {
    const rootToUse = getRootToUse();
    if (!rootToUse || rootToUse.trim() === '') {
      addToast('Repository root is not set. Please configure it first.', 'error');
      return;
    }

    try {
      await invoke<ServiceStatus>('start_service', { serviceName, repoRoot: rootToUse });
      setViewModeForService(serviceName, 'logs');
      handleShowLogs(serviceName, true);
      setAutoScroll(prev => ({ ...prev, [serviceName]: true }));
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      addToast(`${config?.displayName || serviceName} started`, 'success');
      await refreshServices();
    } catch (error) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      addToast(`Failed to start ${config?.displayName || serviceName}: ${error}`, 'error');
    }
  };

  const stopService = async (serviceName: string) => {
    try {
      await invoke('stop_service', { serviceName });
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      addToast(`${config?.displayName || serviceName} stopped`, 'info');
      await refreshServices();
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

    const rootToUse = getRootToUse();
    if (!rootToUse || rootToUse.trim() === '') {
      addToast('Repository root is not set. Please configure it first.', 'error');
      return;
    }

    const serviceStatus = services.find(s => s.name === serviceName);
    const wasRunning = serviceStatus?.running || false;

    if (wasRunning) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      addToast(`Stopping ${config?.displayName || serviceName} before rebuild...`, 'info', 2000);
      try {
        await stopService(serviceName);
        await new Promise(resolve => setTimeout(resolve, 1500));
        await refreshServices();
        const stillRunning = services.find(s => s.name === serviceName)?.running;
        if (stillRunning) {
          addToast(`Service ${serviceName} is still running. Please stop it manually and try again.`, 'error', 5000);
          return;
        }
      } catch (error) {
        addToast(`Failed to stop ${serviceName} before rebuild: ${error}`, 'error');
        return;
      }
    }

    try {
      const success = await prebuildService(serviceName, rootToUse, setViewModeForService, handleShowLogs, true);
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      if (success) {
        addToast(`${config?.displayName || serviceName} rebuilt successfully`, 'success');
      } else {
        addToast(`Failed to rebuild ${config?.displayName || serviceName}`, 'error');
      }
    } catch (error) {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find(
        (s: any) => s.name === serviceName
      );
      addToast(`Failed to rebuild ${config?.displayName || serviceName}`, 'error');
    }
  };

  const startAllServices = async () => {
    const servicesToStart = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).filter(
      (config: any) => {
        const status = services.find(s => s.name === config.name);
        const currentBuild = buildStatus[config.name];
        return !status?.running && currentBuild?.status !== 'building';
      }
    );

    if (servicesToStart.length === 0) {
      const buildingServices = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).filter(
        (config: any) => {
          const currentBuild = buildStatus[config.name];
          return currentBuild?.status === 'building';
        }
      );

      if (buildingServices.length > 0) {
        addToast(
          `Cannot start services: ${buildingServices.map((s: any) => s.displayName).join(', ')} ${
            buildingServices.length === 1 ? 'is' : 'are'
          } currently building. Please wait for the build to complete.`,
          'warning',
          5000
        );
      } else {
        addToast('All services are already running or configured for remote environments!', 'info');
      }
      return;
    }

    addToast(`Starting ${servicesToStart.length} service(s)... This may take a minute.`, 'info', 8000);

    try {
      const rootToUse = getRootToUse();
      for (let i = 0; i < servicesToStart.length; i++) {
        const service = servicesToStart[i];
        try {
          await invoke<ServiceStatus>('start_service', { serviceName: service.name, repoRoot: rootToUse });
          handleShowLogs(service.name, true);
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

  return {
    startService,
    stopService,
    rebuildService,
    startAllServices,
    stopAllServices,
  };
}

