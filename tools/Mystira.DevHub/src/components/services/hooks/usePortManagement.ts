import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import { getServiceConfigs } from '../utils/serviceUtils';

export function usePortManagement(
  repoRoot: string,
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>,
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string },
  onToast: (message: string, type: 'success' | 'error' | 'warning' | 'info') => void
) {
  const [customPorts, setCustomPorts] = useState<Record<string, number>>(() => {
    const saved = localStorage.getItem('servicePorts');
    return saved ? JSON.parse(saved) : {};
  });

  const updateServicePort = async (
    serviceName: string,
    port: number,
    persistToFile: boolean = false
  ) => {
    if (port < 1 || port > 65535) {
      onToast('Port must be between 1 and 65535', 'error');
      return;
    }
    
    const configs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const config = configs.find((c) => c.name === serviceName);
    const displayName = config?.displayName || serviceName;
    
    // Check for conflicts
    const conflictingService = configs.find((c) => c.name !== serviceName && c.port === port);
    if (conflictingService) {
      const confirmed = window.confirm(
        `Port ${port} is already used by "${conflictingService.displayName}".\n\n` +
        `Would you like to auto-assign a new port?`
      );
      
      if (confirmed) {
        try {
          const newPort = await invoke<number>('find_available_port', { startPort: port });
          port = newPort;
          onToast(`Auto-assigned port ${newPort} to avoid conflict`, 'info');
        } catch (error) {
          onToast(`Failed to find available port: ${error}`, 'error');
          return;
        }
      } else {
        return;
      }
    }
    
    // Update in memory
    const newPorts = { ...customPorts, [serviceName]: port };
    setCustomPorts(newPorts);
    localStorage.setItem('servicePorts', JSON.stringify(newPorts));
    
    // Persist to file if requested
    if (persistToFile && repoRoot) {
      try {
        await invoke('update_service_port', {
          serviceName,
          repoRoot,
          newPort: port,
        });
        onToast(`Port ${port} updated for ${displayName} and saved to launchSettings.json`, 'success');
      } catch (error) {
        onToast(`Port updated in UI but failed to save to file: ${error}`, 'warning');
      }
    } else if (!persistToFile) {
      // Show confirmation dialog to persist
      const confirmed = window.confirm(
        `Port ${port} updated for ${displayName}.\n\n` +
        `Would you like to save this to launchSettings.json?`
      );
      
      if (confirmed && repoRoot) {
        try {
          await invoke('update_service_port', {
            serviceName,
            repoRoot,
            newPort: port,
          });
          onToast(`Port ${port} saved to launchSettings.json`, 'success');
        } catch (error) {
          onToast(`Failed to save port to file: ${error}`, 'error');
        }
      }
    }
  };

  const loadPortsFromFiles = async (repoRoot: string) => {
    try {
      const services = ['api', 'admin-api', 'pwa'];
      const portPromises = services.map(async (serviceName) => {
        try {
          const port = await invoke<number>('get_service_port', {
            serviceName,
            repoRoot,
          });
          if (port && port > 0) {
            setCustomPorts(prev => ({ ...prev, [serviceName]: port }));
          }
        } catch (error) {
          console.warn(`Failed to load port for ${serviceName}:`, error);
        }
      });
      
      await Promise.all(portPromises);
      
      // Check for conflicts after loading
      const configs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
      const portMap = new Map<number, string[]>();
      
      configs.forEach(config => {
        const port = config.port;
        if (!portMap.has(port)) {
          portMap.set(port, []);
        }
        portMap.get(port)!.push(config.name);
      });
      
      portMap.forEach((services, port) => {
        if (services.length > 1) {
          onToast(`Port conflict detected: Port ${port} used by ${services.join(', ')}`, 'warning');
        }
      });
    } catch (error) {
      console.error('Failed to load ports from files:', error);
    }
  };

  return {
    customPorts,
    updateServicePort,
    loadPortsFromFiles,
  };
}

