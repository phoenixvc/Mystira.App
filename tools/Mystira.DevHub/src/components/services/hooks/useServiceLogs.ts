import { UnlistenFn, listen } from '@tauri-apps/api/event';
import { useEffect, useRef, useState } from 'react';
import { ServiceLog } from '../types';

export function useServiceLogs() {
  const [logs, setLogs] = useState<Record<string, ServiceLog[]>>({});
  const [logFilters, setLogFilters] = useState<Record<string, {
    search: string;
    type: 'all' | 'stdout' | 'stderr';
    source?: 'all' | 'build' | 'run';
    severity?: 'all' | 'errors' | 'warnings' | 'info';
  }>>({});
  const [autoScroll, setAutoScroll] = useState<Record<string, boolean>>({});
  const [maxLogs, setMaxLogs] = useState<number>(() => {
    const saved = localStorage.getItem('logRetentionLimit');
    return saved ? parseInt(saved, 10) : 10000; // Default 10k logs
  });
  const logListenerRef = useRef<UnlistenFn | null>(null);

  useEffect(() => {
    const setupLogListener = async () => {
      if (logListenerRef.current) {
        return;
      }

      const unlisten = await listen<{ service: string; type: 'stdout' | 'stderr'; source?: 'build' | 'run'; message: string }>(
        'service-log',
        (event) => {
          const { service, type, source, message } = event.payload;
          
          setLogs(prevLogs => {
            const serviceLogs = prevLogs[service] || [];
            const newLog: ServiceLog = {
              service,
              type,
              source: source || 'run', // Default to 'run' for backward compatibility
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
            
            // Apply log retention limit
            const updatedLogs = [...serviceLogs, newLog];
            const trimmedLogs = updatedLogs.length > maxLogs 
              ? updatedLogs.slice(-maxLogs) // Keep only the most recent logs
              : updatedLogs;
            
            return {
              ...prevLogs,
              [service]: trimmedLogs,
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
    const filter = logFilters[serviceName] || { search: '', type: 'all', source: 'all', severity: 'all' };
    
    return serviceLogs.filter(log => {
      const matchesSearch = !filter.search || 
        log.message.toLowerCase().includes(filter.search.toLowerCase());
      const matchesType = filter.type === 'all' || log.type === filter.type;
      const matchesSource = !filter.source || filter.source === 'all' || log.source === filter.source;
      
      // Severity filter: detect errors and warnings in log messages
      let matchesSeverity = true;
      if (filter.severity && filter.severity !== 'all') {
        const messageLower = log.message.toLowerCase();
        if (filter.severity === 'errors') {
          matchesSeverity = log.type === 'stderr' || 
            messageLower.includes('error') || 
            messageLower.includes('failed') ||
            messageLower.includes('exception') ||
            messageLower.includes('fatal');
        } else if (filter.severity === 'warnings') {
          matchesSeverity = messageLower.includes('warning') || 
            messageLower.includes('warn') ||
            messageLower.includes('deprecated');
        } else if (filter.severity === 'info') {
          matchesSeverity = log.type === 'stdout' && 
            !messageLower.includes('error') && 
            !messageLower.includes('warning') &&
            !messageLower.includes('failed');
        }
      }
      
      return matchesSearch && matchesType && matchesSource && matchesSeverity;
    });
  };

  const clearLogs = (serviceName: string) => {
    setLogs(prev => ({ ...prev, [serviceName]: [] }));
  };

  const updateMaxLogs = (newLimit: number) => {
    setMaxLogs(newLimit);
    localStorage.setItem('logRetentionLimit', newLimit.toString());
    
    // Trim existing logs if needed
    setLogs(prevLogs => {
      const updated: Record<string, ServiceLog[]> = {};
      Object.entries(prevLogs).forEach(([service, serviceLogs]) => {
        updated[service] = serviceLogs.length > newLimit 
          ? serviceLogs.slice(-newLimit)
          : serviceLogs;
      });
      return updated;
    });
  };

  return {
    logs,
    logFilters,
    autoScroll,
    maxLogs,
    setLogFilters,
    setAutoScroll,
    getServiceLogs,
    clearLogs,
    updateMaxLogs,
  };
}

