import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import type { ServiceStatus } from '../types';

export function useServiceRefresh() {
  const [services, setServices] = useState<ServiceStatus[]>([]);

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

  return {
    services,
    setServices,
    refreshServices,
  };
}

