import { UnlistenFn, listen } from '@tauri-apps/api/event';
import { useEffect, useRef, useState } from 'react';
import { ServiceLog } from '../types';

export function useServiceLogs() {
  const [logs, setLogs] = useState<Record<string, ServiceLog[]>>({});
  const [logFilters, setLogFilters] = useState<Record<string, {
    search: string;
    type: 'all' | 'stdout' | 'stderr';
  }>>({});
  const [autoScroll, setAutoScroll] = useState<Record<string, boolean>>({});
  const logListenerRef = useRef<UnlistenFn | null>(null);

  useEffect(() => {
    const setupLogListener = async () => {
      if (logListenerRef.current) {
        return;
      }

      const unlisten = await listen<{ service: string; type: 'stdout' | 'stderr'; message: string }>(
        'service-log',
        (event) => {
          const { service, type, message } = event.payload;
          
          setLogs(prevLogs => {
            const serviceLogs = prevLogs[service] || [];
            const newLog: ServiceLog = {
              service,
              type,
              message,
              timestamp: Date.now(),
            };
            
            // Deduplication: check last 5 entries
            const recentLogs = serviceLogs.slice(-5);
            const isDuplicate = recentLogs.some(log => 
              log.message === message && 
              Math.abs(log.timestamp - newLog.timestamp) < 500
            );
            
            if (isDuplicate) {
              return prevLogs;
            }
            
            return {
              ...prevLogs,
              [service]: [...serviceLogs, newLog],
            };
          });
        }
      );
      
      logListenerRef.current = unlisten;
    };

    setupLogListener();

    return () => {
      if (logListenerRef.current) {
        logListenerRef.current();
        logListenerRef.current = null;
      }
    };
  }, []);

  const getServiceLogs = (serviceName: string): ServiceLog[] => {
    const serviceLogs = logs[serviceName] || [];
    const filter = logFilters[serviceName] || { search: '', type: 'all' };
    
    return serviceLogs.filter(log => {
      const matchesSearch = !filter.search || 
        log.message.toLowerCase().includes(filter.search.toLowerCase());
      const matchesType = filter.type === 'all' || log.type === filter.type;
      return matchesSearch && matchesType;
    });
  };

  const clearLogs = (serviceName: string) => {
    setLogs(prev => ({ ...prev, [serviceName]: [] }));
  };

  return {
    logs,
    logFilters,
    autoScroll,
    setLogFilters,
    setAutoScroll,
    getServiceLogs,
    clearLogs,
  };
}

