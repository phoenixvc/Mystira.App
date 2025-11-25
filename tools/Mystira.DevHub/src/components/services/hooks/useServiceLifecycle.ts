import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import { BuildStatus, ServiceStatus } from '../types';

interface UseServiceLifecycleProps {
  repoRoot: string;
  customPorts: Record<string, number>;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  getEnvironmentUrls: (serviceName: string) => { dev?: string; prod?: string };
  onBuildStatusChange: (serviceName: string, status: BuildStatus) => void;
}

export function useServiceLifecycle({
  repoRoot,
  customPorts,
  serviceEnvironments,
  getEnvironmentUrls,
  onBuildStatusChange,
}: UseServiceLifecycleProps) {
  const [loading, setLoading] = useState<Record<string, boolean>>({});
  const [statusMessage, setStatusMessage] = useState<Record<string, string>>({});
  const [services, setServices] = useState<ServiceStatus[]>([]);

  const prebuildService = async (serviceName: string) => {
    const startTime = Date.now();
    
    onBuildStatusChange(serviceName, {
      status: 'building',
      progress: 0,
      message: 'Initializing build...',
    });
    
    const progressInterval = setInterval(() => {
      const elapsed = Date.now() - startTime;
      const estimatedProgress = Math.min(90, 10 + (elapsed / 45000) * 80);
      onBuildStatusChange(serviceName, {
        status: 'building',
        progress: estimatedProgress,
        message: 'Building...',
      });
    }, 500);
    
    try {
      await invoke('prebuild_service', {
        serviceName,
        repoRoot,
        appHandle: null,
      });
      
      clearInterval(progressInterval);
      const duration = Date.now() - startTime;
      onBuildStatusChange(serviceName, {
        status: 'success',
        progress: 100,
        lastBuildTime: Date.now(),
        buildDuration: duration,
        message: 'Build completed successfully',
      });
    } catch (error) {
      clearInterval(progressInterval);
      onBuildStatusChange(serviceName, {
        status: 'failed',
        lastBuildTime: Date.now(),
        message: `Build failed: ${error}`,
      });
      throw error;
    }
  };

  const startService = async (serviceName: string) => {
    setLoading(prev => ({ ...prev, [serviceName]: true }));
    setStatusMessage(prev => ({ ...prev, [serviceName]: 'Starting...' }));
    
    try {
      const environment = serviceEnvironments[serviceName] || 'local';
      const envUrls = getEnvironmentUrls(serviceName);
      
      let url: string | undefined;
      if (environment === 'dev' && envUrls.dev) {
        url = envUrls.dev;
      } else if (environment === 'prod' && envUrls.prod) {
        url = envUrls.prod;
      } else {
        const port = customPorts[serviceName] || 7096;
        url = `https://localhost:${port}/swagger`;
      }
      
      const result = await invoke<{ success: boolean; message?: string }>('start_service', {
        serviceName,
        repoRoot,
        port: customPorts[serviceName] || undefined,
      });
      
      if (result.success) {
        setServices(prev => {
          const existing = prev.find(s => s.name === serviceName);
          if (existing) {
            return prev.map(s => s.name === serviceName ? { ...s, running: true, url } : s);
          }
          return [...prev, { name: serviceName, running: true, url }];
        });
        setStatusMessage(prev => ({ ...prev, [serviceName]: 'Started successfully' }));
      } else {
        throw new Error(result.message || 'Failed to start service');
      }
    } catch (error) {
      setStatusMessage(prev => ({ ...prev, [serviceName]: `Error: ${error}` }));
      throw error;
    } finally {
      setLoading(prev => ({ ...prev, [serviceName]: false }));
    }
  };

  const stopService = async (serviceName: string) => {
    setLoading(prev => ({ ...prev, [serviceName]: true }));
    setStatusMessage(prev => ({ ...prev, [serviceName]: 'Stopping...' }));
    
    try {
      await invoke('stop_service', { serviceName });
      setServices(prev => prev.map(s => s.name === serviceName ? { ...s, running: false } : s));
      setStatusMessage(prev => ({ ...prev, [serviceName]: 'Stopped' }));
    } catch (error) {
      setStatusMessage(prev => ({ ...prev, [serviceName]: `Error: ${error}` }));
      throw error;
    } finally {
      setLoading(prev => ({ ...prev, [serviceName]: false }));
    }
  };

  const refreshServices = async () => {
    try {
      const result = await invoke<ServiceStatus[]>('get_service_statuses', {});
      setServices(result);
    } catch (error) {
      console.error('Failed to refresh services:', error);
    }
  };

  return {
    services,
    loading,
    statusMessage,
    prebuildService,
    startService,
    stopService,
    refreshServices,
  };
}

