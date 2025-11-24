import { open as openDialog } from '@tauri-apps/api/dialog';
import { listen, UnlistenFn } from '@tauri-apps/api/event';
import { open } from '@tauri-apps/api/shell';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcut';
import { Toast, ToastContainer, useToast } from './Toast';

interface ServiceStatus {
  name: string;
  running: boolean;
  port?: number;
  url?: string;
  health?: 'healthy' | 'unhealthy' | 'unknown';
  portConflict?: boolean;
}

interface BuildStatus {
  status: 'idle' | 'building' | 'success' | 'failed';
  progress?: number; // 0-100
  lastBuildTime?: number; // timestamp
  buildDuration?: number; // milliseconds
  message?: string;
}

interface ServiceLog {
  service: string;
  type: 'stdout' | 'stderr';
  message: string;
  timestamp: number;
}

function ServiceManager() {
  const [services, setServices] = useState<ServiceStatus[]>([]);
  const [loading, setLoading] = useState<Record<string, boolean>>({});
  const [statusMessage, setStatusMessage] = useState<Record<string, string>>({});
  const [buildStatus, setBuildStatus] = useState<Record<string, BuildStatus>>({});
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [currentBranch, setCurrentBranch] = useState<string>('');
  const [useCurrentBranch, setUseCurrentBranch] = useState<boolean>(false);
  const [customPorts, setCustomPorts] = useState<Record<string, number>>(() => {
    // Load from localStorage
    const saved = localStorage.getItem('servicePorts');
    return saved ? JSON.parse(saved) : {};
  });
  const [logs, setLogs] = useState<Record<string, ServiceLog[]>>({});
  const [selectedService, setSelectedService] = useState<string | null>(null);
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});
  const [viewMode, setViewMode] = useState<Record<string, 'logs' | 'webview' | 'split'>>({});
  const [maximizedService, setMaximizedService] = useState<string | null>(null);
  const [webviewErrors, setWebviewErrors] = useState<Record<string, boolean>>({});
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [logFilters, setLogFilters] = useState<Record<string, {
    search: string;
    type: 'all' | 'stdout' | 'stderr';
  }>>({});
  const [autoScroll, setAutoScroll] = useState<Record<string, boolean>>({});
  const logEndRef = useRef<HTMLDivElement>(null);
  const logListenerRef = useRef<UnlistenFn | null>(null);
  const { showToast } = useToast();

  const addToast = (message: string, type: Toast['type'] = 'info', duration: number = 5000) => {
    const toast = showToast(message, type, duration);
    setToasts((prev) => [...prev, toast]);
  };

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  // Prebuild function
  const prebuildService = async (serviceName: string, repoRoot: string) => {
    const startTime = Date.now();
    
    // Initialize logs for this service BEFORE starting build
    setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
    setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
    // Set view mode to logs during build
    if (!viewMode[serviceName]) {
      setViewModeForService(serviceName, 'logs');
    }
    
    setBuildStatus(prev => ({
      ...prev,
      [serviceName]: {
        status: 'building',
        progress: 0,
        message: 'Initializing build...',
      },
    }));
    
    // Progress estimation - update every 500ms
    const progressInterval = setInterval(() => {
      const elapsed = Date.now() - startTime;
      // Estimate progress: assume builds take 30-60 seconds on average
      // Start at 10%, reach 90% by 45 seconds
      const estimatedProgress = Math.min(90, 10 + (elapsed / 45000) * 80);
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          ...prev[serviceName],
          progress: Math.floor(estimatedProgress),
          message: `Building... (${Math.floor(estimatedProgress)}%)`,
        },
      }));
    }, 500);
    
    try {
      await invoke('prebuild_service', {
        serviceName,
        repoRoot,
      });
      
      clearInterval(progressInterval);
      const duration = Date.now() - startTime;
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          status: 'success',
          progress: 100,
          lastBuildTime: Date.now(),
          buildDuration: duration,
          message: `Built in ${(duration / 1000).toFixed(1)}s`,
        },
      }));
    } catch (error: any) {
      clearInterval(progressInterval);
      const duration = Date.now() - startTime;
      setBuildStatus(prev => ({
        ...prev,
        [serviceName]: {
          status: 'failed',
          progress: 0,
          lastBuildTime: Date.now(),
          buildDuration: duration,
          message: `Build failed: ${error?.message || error}`,
        },
      }));
      console.error(`Prebuild failed for ${serviceName}:`, error);
    }
  };
  
  const prebuildAllServices = async (repoRoot: string) => {
    const rootToUse = useCurrentBranch && currentBranch 
      ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
      : repoRoot;
    
    // Prebuild all services in parallel
    const prebuildPromises = serviceConfigs.map(config =>
      prebuildService(config.name, rootToUse).catch(err => {
        console.error(`Failed to prebuild ${config.name}:`, err);
      })
    );
    
    await Promise.allSettled(prebuildPromises);
  };

  // Load ports from launchSettings.json files
  const loadPortsFromFiles = async (repoRoot: string) => {
    try {
      const loadedPorts: Record<string, number> = {};
      const baseConfigs = [
        { name: 'api', displayName: 'API', defaultPort: 7096, isHttps: true, path: '/swagger' },
        { name: 'admin-api', displayName: 'Admin API', defaultPort: 7097, isHttps: true, path: '/swagger' },
        { name: 'pwa', displayName: 'PWA', defaultPort: 7000, isHttps: false, path: '' },
      ];
      
      for (const config of baseConfigs) {
        try {
          const port = await invoke<number>('get_service_port', {
            serviceName: config.name,
            repoRoot,
          });
          loadedPorts[config.name] = port;
        } catch (error) {
          console.warn(`Failed to load port for ${config.name}:`, error);
        }
      }
      
      // Merge with any existing custom ports (custom ports take precedence)
      const mergedPorts = { ...loadedPorts, ...customPorts };
      setCustomPorts(mergedPorts);
      localStorage.setItem('servicePorts', JSON.stringify(mergedPorts));
    } catch (error) {
      console.error('Failed to load ports from files:', error);
    }
  };

  // Initialize and prebuild on mount
  useEffect(() => {
    const initialize = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: root });
          setCurrentBranch(branch);
        } catch (error) {
          console.error('Failed to get current branch:', error);
        }
        
        // Load ports from launchSettings.json files
        await loadPortsFromFiles(root);
        
        await refreshServices();
        
        // Check for port conflicts and auto-resolve (after state updates)
        setTimeout(async () => {
          // Re-read customPorts from state after loadPortsFromFiles updates it
          const currentPorts = JSON.parse(localStorage.getItem('servicePorts') || '{}');
          const baseConfigs = [
            { name: 'api', displayName: 'API', defaultPort: 7096, isHttps: true, path: '/swagger' },
            { name: 'admin-api', displayName: 'Admin API', defaultPort: 7097, isHttps: true, path: '/swagger' },
            { name: 'pwa', displayName: 'PWA', defaultPort: 7000, isHttps: false, path: '' },
          ];
          
          const configs = baseConfigs.map(config => {
            const port = currentPorts[config.name] || config.defaultPort;
            const protocol = config.isHttps ? 'https' : 'http';
            const url = `${protocol}://localhost:${port}${config.path}`;
            return { ...config, port, url };
          });
          
          const portMap = new Map<number, string[]>();
          for (const config of configs) {
            const existing = portMap.get(config.port) || [];
            portMap.set(config.port, [...existing, config.name]);
          }
          
          const conflicts: Array<{ port: number; services: string[] }> = [];
          for (const [port, services] of portMap.entries()) {
            if (services.length > 1) {
              conflicts.push({ port, services });
            }
          }
          
          for (const conflict of conflicts) {
            const isInUse = await invoke<boolean>('check_port_available', { port: conflict.port });
            if (!isInUse || conflict.services.length > 1) {
              for (let i = 1; i < conflict.services.length; i++) {
                const serviceName = conflict.services[i];
                const config = configs.find(c => c.name === serviceName);
                if (config) {
                  try {
                    const newPort = await invoke<number>('find_available_port', { 
                      startPort: config.defaultPort 
                    });
                    
                    const confirmed = window.confirm(
                      `Port conflict detected!\n\n` +
                      `Service "${config.displayName}" is using port ${conflict.port} which conflicts with ${conflict.services[0]}.\n\n` +
                      `Would you like to:\n` +
                      `1. Auto-assign port ${newPort} to "${config.displayName}"?\n` +
                      `2. Persist this change to launchSettings.json?\n\n` +
                      `Click OK to apply and persist, or Cancel to keep current port.`
                    );
                    
                    if (confirmed) {
                      await updateServicePort(serviceName, newPort, true);
                    }
                  } catch (error) {
                    console.error(`Failed to find available port for ${serviceName}:`, error);
                  }
                }
              }
            }
          }
        }, 500);
        
        // Prebuild all services in the background after a short delay
        setTimeout(() => {
          prebuildAllServices(root);
        }, 1000);
      } catch (error) {
        console.error('Failed to get repo root:', error);
        addToast('Warning: Not running in Tauri. Please use the Tauri application window.', 'warning', 5000);
        setRepoRoot('C:\\Users\\smitj\\repos\\Mystira.App');
      }
    };
    
    initialize();
    const interval = setInterval(refreshServices, 2000);
    return () => clearInterval(interval);
  }, []);

  // Listen for service logs
  useEffect(() => {
    let isMounted = true;
    
    const setupLogListener = async () => {
      // Clean up any existing listener first
      if (logListenerRef.current) {
        logListenerRef.current();
        logListenerRef.current = null;
      }
      
      const unlisten = await listen<ServiceLog>('service-log', (event) => {
        // Only process logs if component is still mounted
        if (!isMounted) return;
        
        const log = { 
          ...event.payload, 
          timestamp: (event.payload as any).timestamp || Date.now() 
        };
        setLogs((prevLogs) => {
          const serviceLogs = prevLogs[log.service] || [];
          
          // Deduplicate: check the last 5 log entries for the same message within 500ms
          // This prevents duplicate logs from stdout/stderr or multiple listeners
          const recentLogs = serviceLogs.slice(-5);
          const isDuplicate = recentLogs.some(recentLog => 
            recentLog.message === log.message && 
            Math.abs(recentLog.timestamp - log.timestamp) < 500
          );
          
          if (isDuplicate) {
            // Skip duplicate log
            return prevLogs;
          }
          
          return {
            ...prevLogs,
            [log.service]: [...serviceLogs, log].slice(-1000), // Keep last 1000 lines
          };
        });
      });
      
      logListenerRef.current = unlisten;
    };

    setupLogListener();

    // Cleanup function
    return () => {
      isMounted = false;
      if (logListenerRef.current) {
        logListenerRef.current();
        logListenerRef.current = null;
      }
    };
  }, []);

  // Auto-scroll logs
  useEffect(() => {
    if (logEndRef.current && selectedService && autoScroll[selectedService] !== false) {
      logEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs, selectedService, autoScroll]);

  const refreshServices = async () => {
    try {
      const statuses = await invoke<ServiceStatus[]>('get_service_status');
      
      // Check port conflicts and health for each service
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
      
      setServices(enrichedStatuses);
    } catch (error) {
      console.error('Failed to get service status:', error);
    }
  };

  const pickRepoRoot = async () => {
    try {
      const selected = await openDialog({
        directory: true,
        multiple: false,
        defaultPath: repoRoot,
      });
      if (selected && typeof selected === 'string') {
        setRepoRoot(selected);
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: selected });
          setCurrentBranch(branch);
          addToast(`Repository root updated. Branch: ${branch}`, 'success');
        } catch (error) {
          console.error('Failed to get current branch:', error);
          setCurrentBranch('');
        }
      }
    } catch (error) {
      console.error('Failed to pick directory:', error);
      addToast('Failed to select directory', 'error');
    }
  };

  const startService = async (serviceName: string) => {
    setLoading({ ...loading, [serviceName]: true });
    setStatusMessage(prev => ({ ...prev, [serviceName]: 'Preparing...' }));
    
    // Initialize logs BEFORE starting (so we capture build output)
    setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
    setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
    setSelectedService(serviceName);
    setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
    
    try {
      const config = serviceConfigs.find(s => s.name === serviceName);
      const displayName = config?.displayName || serviceName;
      
      // Check for port conflicts before starting
      if (config?.port) {
        setStatusMessage(prev => ({ ...prev, [serviceName]: 'Checking port...' }));
        try {
          const available = await invoke<boolean>('check_port_available', { port: config.port });
          if (!available) {
            addToast(`Port ${config.port} is already in use!`, 'warning', 7000);
            setLoading({ ...loading, [serviceName]: false });
            setStatusMessage(prev => {
              const updated = { ...prev };
              delete updated[serviceName];
              return updated;
            });
            return;
          }
        } catch (portError) {
          console.warn('Port check failed, continuing anyway:', portError);
        }
      }
      
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;
      
      setStatusMessage(prev => ({ ...prev, [serviceName]: `Building ${displayName}...` }));
      
      await invoke<ServiceStatus>('start_service', {
        serviceName,
        repoRoot: rootToUse,
      });
      
      setStatusMessage(prev => ({ ...prev, [serviceName]: `Starting ${displayName}...` }));
      
      await refreshServices();
      
      // Default to logs view when service starts
      if (!viewMode[serviceName]) {
        setViewModeForService(serviceName, 'logs');
      }
      
      setStatusMessage(prev => ({ ...prev, [serviceName]: 'Running' }));
      setTimeout(() => {
        setStatusMessage(prev => {
          const updated = { ...prev };
          delete updated[serviceName];
          return updated;
        });
      }, 2000);
      
      addToast(`${displayName} started successfully`, 'success');
    } catch (error: any) {
      const errorMessage = error?.message || String(error);
      console.error(`Failed to start ${serviceName}:`, error);
      
      setStatusMessage(prev => ({ ...prev, [serviceName]: 'Failed' }));
      setTimeout(() => {
        setStatusMessage(prev => {
          const updated = { ...prev };
          delete updated[serviceName];
          return updated;
        });
      }, 3000);
      
      // Provide more helpful error messages
      if (errorMessage.includes('__TAURI_IPC__') || errorMessage.includes('not a function')) {
        addToast(
          `Tauri API error: Make sure you're running DevHub through Tauri (not in a browser). Restart the app if the issue persists.`,
          'error',
          10000
        );
      } else {
        addToast(`Failed to start ${serviceName}: ${errorMessage}`, 'error');
      }
    } finally {
      setLoading({ ...loading, [serviceName]: false });
    }
  };

  const stopService = async (serviceName: string) => {
    setLoading({ ...loading, [serviceName]: true });
    try {
      const config = serviceConfigs.find(s => s.name === serviceName);
      await invoke('stop_service', { serviceName });
      await refreshServices();
      addToast(`${config?.displayName || serviceName} stopped`, 'info');
    } catch (error) {
      addToast(`Failed to stop ${serviceName}: ${error}`, 'error');
    } finally {
      setLoading({ ...loading, [serviceName]: false });
    }
  };

  const openInBrowser = async (url: string) => {
    try {
      await open(url);
    } catch (error) {
      console.error('Failed to open URL:', error);
      addToast('Failed to open URL in browser', 'error');
    }
  };


  const toggleLogs = (serviceName: string) => {
    setShowLogs((prev) => ({ ...prev, [serviceName]: !prev[serviceName] }));
    if (!showLogs[serviceName]) {
      setSelectedService(serviceName);
      setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
    }
  };

  const setViewModeForService = (serviceName: string, mode: 'logs' | 'webview' | 'split') => {
    setViewMode((prev) => ({ ...prev, [serviceName]: mode }));
    if (mode === 'logs' || mode === 'split') {
      setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
      setSelectedService(serviceName);
      setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
    }
    // When switching to webview-only, we can hide logs to save space
    if (mode === 'webview') {
      setShowLogs((prev) => ({ ...prev, [serviceName]: false }));
    }
  };

  const toggleMaximize = (serviceName: string) => {
    if (maximizedService === serviceName) {
      setMaximizedService(null);
    } else {
      setMaximizedService(serviceName);
    }
  };

  const clearLogs = (serviceName: string) => {
    setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
    addToast(`Cleared logs for ${serviceName}`, 'info');
  };

  const startAllServices = async () => {
    const servicesToStart = serviceConfigs.filter(config => {
      const status = getServiceStatus(config.name);
      return !status?.running;
    });

    if (servicesToStart.length === 0) {
      addToast('All services are already running!', 'info');
      return;
    }

    // Initialize loading and status messages
    const initialLoading = { ...loading, ...Object.fromEntries(servicesToStart.map(s => [s.name, true])) };
    const initialStatus = Object.fromEntries(servicesToStart.map(s => [s.name, 'Preparing...']));
    setLoading(initialLoading);
    setStatusMessage(initialStatus);
    
    addToast(`Starting ${servicesToStart.length} service(s)... This may take a minute.`, 'info', 8000);
    
    try {
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;

      // Check for port conflicts first
      setStatusMessage(prev => ({ ...prev, ...Object.fromEntries(servicesToStart.map(s => [s.name, 'Checking ports...'])) }));
      const portChecks = await Promise.all(
        servicesToStart.map(async (service) => {
          const available = await invoke<boolean>('check_port_available', { port: service.port });
          return { service: service.name, available, port: service.port };
        })
      );

      const conflicts = portChecks.filter(check => !check.available);
      if (conflicts.length > 0) {
        addToast(`Port conflicts detected: ${conflicts.map(c => c.port).join(', ')}`, 'warning', 7000);
      }

      // Start services sequentially with progress updates for better UX
      const results: Array<{ service: string; success: boolean; error?: any }> = [];
      
      for (let i = 0; i < servicesToStart.length; i++) {
        const service = servicesToStart[i];
        const displayName = service.displayName || service.name;
        
        try {
          setStatusMessage(prev => ({ ...prev, [service.name]: `Building ${displayName}...` }));
          
          await invoke<ServiceStatus>('start_service', {
            serviceName: service.name,
            repoRoot: rootToUse,
          });
          
          setStatusMessage(prev => ({ ...prev, [service.name]: `Starting ${displayName}...` }));
          
          setLogs((prevLogs) => ({ ...prevLogs, [service.name]: [] }));
          setShowLogs((prev) => ({ ...prev, [service.name]: true }));
          setAutoScroll((prev) => ({ ...prev, [service.name]: true }));
          
          // Small delay to show the status update
          await new Promise(resolve => setTimeout(resolve, 500));
          
          setStatusMessage(prev => ({ ...prev, [service.name]: 'Running' }));
          results.push({ service: service.name, success: true });
          
          addToast(`${displayName} started (${i + 1}/${servicesToStart.length})`, 'success', 3000);
        } catch (error) {
          console.error(`Failed to start ${service.name}:`, error);
          setStatusMessage(prev => ({ ...prev, [service.name]: 'Failed' }));
          results.push({ service: service.name, success: false, error });
          addToast(`Failed to start ${displayName}`, 'error');
        }
      }

      const failures = results.filter(r => !r.success).map(r => r.service);
      if (failures.length > 0) {
        addToast(`Failed to start: ${failures.join(', ')}`, 'error');
      } else {
        addToast(`All ${servicesToStart.length} service(s) started successfully!`, 'success', 5000);
      }

      await refreshServices();
    } catch (error) {
      addToast(`Failed to start services: ${error}`, 'error');
    } finally {
      // Clear loading and status messages after a short delay
      setTimeout(() => {
        setLoading(prev => {
          const updated = { ...prev };
          servicesToStart.forEach(s => delete updated[s.name]);
          return updated;
        });
        setStatusMessage(prev => {
          const updated = { ...prev };
          servicesToStart.forEach(s => delete updated[s.name]);
          return updated;
        });
      }, 2000);
    }
  };

  const stopAllServices = async () => {
    const runningServices = services.filter(s => s.running);
    
    if (runningServices.length === 0) {
      addToast('No services are running!', 'info');
      return;
    }

    setLoading({ ...loading, ...Object.fromEntries(runningServices.map(s => [s.name, true])) });
    
    try {
      const stopPromises = runningServices.map(service => 
        invoke('stop_service', { serviceName: service.name })
          .catch(error => {
            console.error(`Failed to stop ${service.name}:`, error);
            return { service: service.name, error };
          })
      );

      const results = await Promise.allSettled(stopPromises);
      const failures = results
        .map((result, index) => result.status === 'rejected' ? runningServices[index].name : null)
        .filter(Boolean);

      if (failures.length > 0) {
        addToast(`Failed to stop: ${failures.join(', ')}`, 'error');
      } else {
        addToast(`Stopped ${runningServices.length} service(s)`, 'info');
      }

      await refreshServices();
    } catch (error) {
      addToast(`Failed to stop services: ${error}`, 'error');
    } finally {
      setLoading({ ...loading, ...Object.fromEntries(runningServices.map(s => [s.name, false])) });
    }
  };

  // Get service configs with custom ports
  const getServiceConfigs = () => {
    const baseConfigs = [
      { name: 'api', displayName: 'API', defaultPort: 7096, isHttps: true, path: '/swagger' },
      { name: 'admin-api', displayName: 'Admin API', defaultPort: 7097, isHttps: true, path: '/swagger' },
      { name: 'pwa', displayName: 'PWA', defaultPort: 7000, isHttps: false, path: '' },
    ];
    
    return baseConfigs.map(config => {
      const port = customPorts[config.name] || config.defaultPort;
      const protocol = config.isHttps ? 'https' : 'http';
      const url = `${protocol}://localhost:${port}${config.path}`;
      return {
        ...config,
        port,
        url,
      };
    });
  };
  
  const serviceConfigs = getServiceConfigs();
  
  // Update port for a service
  const updateServicePort = async (serviceName: string, port: number, persistToFile: boolean = false) => {
    if (port < 1 || port > 65535) {
      addToast('Port must be between 1 and 65535', 'error');
      return;
    }
    
    const configs = getServiceConfigs();
    const config = configs.find(s => s.name === serviceName);
    const displayName = config?.displayName || serviceName;
    
    // Check for conflicts
    const conflictingService = configs.find(c => c.name !== serviceName && c.port === port);
    if (conflictingService) {
      const confirmed = window.confirm(
        `Port ${port} is already used by "${conflictingService.displayName}".\n\n` +
        `Would you like to auto-assign a new port?`
      );
      
      if (confirmed) {
        try {
          const newPort = await invoke<number>('find_available_port', { startPort: port });
          port = newPort;
          addToast(`Auto-assigned port ${newPort} to avoid conflict`, 'info');
        } catch (error) {
          addToast(`Failed to find available port: ${error}`, 'error');
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
        addToast(`Port ${port} updated for ${displayName} and saved to launchSettings.json`, 'success');
      } catch (error) {
        addToast(`Port updated in UI but failed to save to file: ${error}`, 'warning');
      }
    } else if (!persistToFile) {
      // Show confirmation dialog to persist
      const confirmed = window.confirm(
        `Port updated to ${port} for "${displayName}".\n\n` +
        `Would you like to persist this change to launchSettings.json?\n\n` +
        `This will update the port in the service's configuration file.`
      );
      
      if (confirmed && repoRoot) {
        try {
          await invoke('update_service_port', {
            serviceName,
            repoRoot,
            newPort: port,
          });
          addToast(`Port ${port} saved to launchSettings.json for ${displayName}`, 'success');
        } catch (error) {
          addToast(`Failed to save to file: ${error}`, 'error');
        }
      }
    } else {
      addToast(`Port updated to ${port} for ${displayName}`, 'success');
    }
  };

  const getServiceStatus = (serviceName: string): ServiceStatus | undefined => {
    return services.find(s => s.name === serviceName);
  };

  const getServiceLogs = (serviceName: string): ServiceLog[] => {
    const allLogs = logs[serviceName] || [];
    const filter = logFilters[serviceName] || { search: '', type: 'all' };
    
    return allLogs.filter(log => {
      const matchesSearch = !filter.search || 
        log.message.toLowerCase().includes(filter.search.toLowerCase());
      const matchesType = filter.type === 'all' || log.type === filter.type;
      return matchesSearch && matchesType;
    });
  };

  const getHealthIndicator = (health?: string) => {
    switch (health) {
      case 'healthy':
        return <span className="text-green-500" title="Service is healthy">●</span>;
      case 'unhealthy':
        return <span className="text-red-500" title="Service is unhealthy">●</span>;
      default:
        return <span className="text-gray-400" title="Health unknown">○</span>;
    }
  };

  // Keyboard shortcuts - must be after function declarations
  useKeyboardShortcuts([
    {
      key: 's',
      ctrl: true,
      shift: true,
      action: startAllServices,
      description: 'Start all services',
    },
    {
      key: 'x',
      ctrl: true,
      shift: true,
      action: stopAllServices,
      description: 'Stop all services',
    },
    {
      key: 'l',
      ctrl: true,
      action: () => {
        if (selectedService) {
          toggleLogs(selectedService);
        }
      },
      description: 'Toggle logs',
    },
    {
      key: 'r',
      ctrl: true,
      action: refreshServices,
      description: 'Refresh services',
    },
  ]);

  const allRunning = services.length === serviceConfigs.length && services.every(s => s.running);
  const anyRunning = services.some(s => s.running);

  return (
    <div className="p-8">
      <ToastContainer toasts={toasts} onClose={removeToast} />
      
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Service Manager</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Keyboard shortcuts: Ctrl+Shift+S (Start All), Ctrl+Shift+X (Stop All), Ctrl+L (Logs), Ctrl+R (Refresh)
          </p>
        </div>
        <div className="flex gap-2">
          {!allRunning && (
            <button
              onClick={startAllServices}
              disabled={loading['all'] || anyRunning}
              className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading['all'] ? 'Starting All...' : 'Start All'}
            </button>
          )}
          {anyRunning && (
            <button
              onClick={stopAllServices}
              disabled={loading['all']}
              className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading['all'] ? 'Stopping All...' : 'Stop All'}
            </button>
          )}
        </div>
      </div>
      
      {/* Repository Root Configuration */}
      <div className="mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
        <h2 className="text-lg font-semibold mb-3 text-gray-900 dark:text-white">Repository Configuration</h2>
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <label className="font-medium text-gray-700 dark:text-gray-300">Repository Root:</label>
            <input
              type="text"
              value={repoRoot}
              onChange={(e) => setRepoRoot(e.target.value)}
              className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
              placeholder="C:\Users\smitj\repos\Mystira.App"
            />
            <button
              onClick={pickRepoRoot}
              className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
            >
              Browse...
            </button>
          </div>
          {currentBranch && (
            <div className="flex items-center gap-3">
              <label className="font-medium text-gray-700 dark:text-gray-300">Current Branch:</label>
              <span className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded">{currentBranch}</span>
              <label className="flex items-center gap-2 text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  checked={useCurrentBranch}
                  onChange={(e) => setUseCurrentBranch(e.target.checked)}
                />
                <span>Use current branch directory</span>
              </label>
            </div>
          )}
        </div>
      </div>
      
      {/* Services */}
      <div className="space-y-4">
        {serviceConfigs.map((config) => {
          const status = getServiceStatus(config.name);
          const isRunning = status?.running || false;
          const isLoading = loading[config.name] || false;
          const statusMsg = statusMessage[config.name];
          const build = buildStatus[config.name];
          const serviceLogs = getServiceLogs(config.name);
          const filter = logFilters[config.name] || { search: '', type: 'all' };
          const isAutoScroll = autoScroll[config.name] !== false;
          
          // Format time since last build
          const formatTimeSince = (timestamp?: number) => {
            if (!timestamp) return null;
            const seconds = Math.floor((Date.now() - timestamp) / 1000);
            if (seconds < 60) return `${seconds}s ago`;
            const minutes = Math.floor(seconds / 60);
            if (minutes < 60) return `${minutes}m ago`;
            const hours = Math.floor(minutes / 60);
            if (hours < 24) return `${hours}h ago`;
            const days = Math.floor(hours / 24);
            return `${days}d ago`;
          };

          return (
            <div
              key={config.name}
              className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 shadow-sm"
            >
              {/* Build Status Indicator */}
              {build && (
                <div className="px-4 pt-3 pb-2 border-b border-gray-200 dark:border-gray-700">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      {build.status === 'building' && (
                        <div className="flex items-center gap-2">
                          <div className="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></div>
                          <span className="text-sm text-gray-600 dark:text-gray-400">
                            {build.message || 'Building...'}
                          </span>
                          {build.progress !== undefined && (
                            <span className="text-xs text-gray-500 dark:text-gray-500">
                              {build.progress}%
                            </span>
                          )}
                        </div>
                      )}
                      {build.status === 'success' && (
                        <div className="flex items-center gap-2">
                          <span className="text-green-600 dark:text-green-400">✓</span>
                          <span className="text-sm text-gray-600 dark:text-gray-400">
                            {build.message || 'Build successful'}
                          </span>
                          {build.lastBuildTime && (
                            <span className="text-xs text-gray-500 dark:text-gray-500">
                              {formatTimeSince(build.lastBuildTime)}
                            </span>
                          )}
                        </div>
                      )}
                      {build.status === 'failed' && (
                        <div className="flex items-center gap-2">
                          <span className="text-red-600 dark:text-red-400">✗</span>
                          <span className="text-sm text-red-600 dark:text-red-400">
                            {build.message || 'Build failed'}
                          </span>
                          {build.lastBuildTime && (
                            <span className="text-xs text-gray-500 dark:text-gray-500">
                              {formatTimeSince(build.lastBuildTime)}
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                    {build.buildDuration && (
                      <span className="text-xs text-gray-500 dark:text-gray-500">
                        Duration: {(build.buildDuration / 1000).toFixed(1)}s
                      </span>
                    )}
                  </div>
                  {build.status === 'building' && build.progress !== undefined && (
                    <div className="mt-2 w-full bg-gray-200 dark:bg-gray-700 rounded-full h-1.5">
                      <div
                        className="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
                        style={{ width: `${build.progress}%` }}
                      ></div>
                    </div>
                  )}
                </div>
              )}
              <div className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <h3 className="text-xl font-semibold text-gray-900 dark:text-white">{config.displayName}</h3>
                      <span
                        className={`px-2 py-1 rounded text-sm ${
                          isRunning
                            ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
                            : isLoading && statusMsg
                            ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300'
                            : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
                        }`}
                      >
                        {isRunning ? 'Running' : isLoading && statusMsg ? statusMsg : 'Stopped'}
                      </span>
                      {isRunning && getHealthIndicator(status?.health)}
                      {status?.portConflict && (
                        <span className="px-2 py-1 rounded text-sm bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300" title="Port conflict detected">
                          ⚠ Port {config.port} in use
                        </span>
                      )}
                      {config.port && (
                        <div className="flex items-center gap-2">
                          <span className="text-sm text-gray-600 dark:text-gray-400">Port:</span>
                          <input
                            type="number"
                            min="1"
                            max="65535"
                            value={config.port}
                            onChange={(e) => {
                              const newPort = parseInt(e.target.value, 10);
                              if (!isNaN(newPort) && newPort !== config.port) {
                                updateServicePort(config.name, newPort, false);
                              }
                            }}
                            onBlur={(e) => {
                              const newPort = parseInt(e.target.value, 10);
                              if (isNaN(newPort) || newPort < 1 || newPort > 65535) {
                                // Reset to current port if invalid
                                e.target.value = config.port.toString();
                              }
                            }}
                            disabled={isRunning}
                            className="w-20 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-blue-500"
                            title={isRunning ? "Stop the service to change port" : "Edit port number"}
                          />
                        </div>
                      )}
                    </div>
                    {isRunning && config.url && (
                      <div className="mt-2 flex gap-2 flex-wrap items-center">
                        <button
                          onClick={() => openInBrowser(config.url!)}
                          className="px-3 py-1 bg-blue-500 text-white rounded text-sm hover:bg-blue-600"
                        >
                          Open in External Browser
                        </button>
                        <div className="flex gap-1 border border-gray-300 dark:border-gray-600 rounded overflow-hidden">
                          <button
                            onClick={() => setViewModeForService(config.name, 'logs')}
                            className={`px-3 py-1 text-sm ${
                              viewMode[config.name] === 'logs' || !viewMode[config.name]
                                ? 'bg-gray-600 text-white'
                                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                            }`}
                            title="Show logs only"
                          >
                            Logs
                          </button>
                          <button
                            onClick={() => setViewModeForService(config.name, 'split')}
                            className={`px-3 py-1 text-sm border-l border-r border-gray-300 dark:border-gray-600 ${
                              viewMode[config.name] === 'split'
                                ? 'bg-gray-600 text-white'
                                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                            }`}
                            title="Show logs and webview side by side"
                          >
                            Split
                          </button>
                          <button
                            onClick={() => setViewModeForService(config.name, 'webview')}
                            className={`px-3 py-1 text-sm ${
                              viewMode[config.name] === 'webview'
                                ? 'bg-gray-600 text-white'
                                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
                            }`}
                            title="Show webview only"
                          >
                            Webview
                          </button>
                        </div>
                        {isRunning && config.url && (
                          <button
                            onClick={() => {
                              const isHttps = config.url?.startsWith('https://');
                              if (isHttps) {
                                openInTauriWindow(config.url, config.displayName);
                              } else {
                                openInBrowser(config.url);
                              }
                            }}
                            className="px-3 py-1 bg-green-500 text-white rounded text-sm hover:bg-green-600"
                            title={config.url?.startsWith('https://') 
                              ? "Open in Tauri window (handles self-signed certificates)" 
                              : "Open in external browser"}
                          >
                            {config.url?.startsWith('https://') ? '🪟 Open Window' : '🌐 Open in Browser'}
                          </button>
                        )}
                        <button
                          onClick={() => toggleMaximize(config.name)}
                          className="px-3 py-1 bg-indigo-500 text-white rounded text-sm hover:bg-indigo-600"
                          title={maximizedService === config.name ? "Restore view" : "Maximize view"}
                        >
                          {maximizedService === config.name ? '↗ Restore' : '⛶ Maximize'}
                        </button>
                        {serviceLogs.length > 0 && (
                          <button
                            onClick={() => clearLogs(config.name)}
                            className="px-3 py-1 bg-red-500 text-white rounded text-sm hover:bg-red-600"
                          >
                            Clear Logs
                          </button>
                        )}
                      </div>
                    )}
                  </div>
                  <div>
                    {isRunning ? (
                      <button
                        onClick={() => stopService(config.name)}
                        disabled={isLoading}
                        className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 disabled:opacity-50"
                      >
                        {isLoading ? 'Stopping...' : 'Stop'}
                      </button>
                    ) : (
                      <button
                        onClick={() => startService(config.name)}
                        disabled={isLoading || status?.portConflict}
                        className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:opacity-50"
                        title={status?.portConflict ? `Port ${config.port} is already in use` : ''}
                      >
                        {isLoading ? (statusMsg || 'Starting...') : 'Start'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
              
              {/* View Content: Logs, Webview, or Split */}
              {/* Show logs during build or when service is running */}
              {((build && build.status === 'building') || (isRunning && config.url)) && (
                <div className={`border-t border-gray-200 dark:border-gray-700 ${
                  maximizedService === config.name 
                    ? 'fixed inset-0 z-50 bg-white dark:bg-gray-900 flex flex-col' 
                    : ''
                }`}>
                  {maximizedService === config.name && (
                    <div className="p-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between bg-gray-100 dark:bg-gray-800">
                      <h3 className="font-semibold text-gray-900 dark:text-white">
                        {config.displayName} - Maximized View
                      </h3>
                      <button
                        onClick={() => toggleMaximize(config.name)}
                        className="px-3 py-1 bg-gray-500 text-white rounded text-sm hover:bg-gray-600"
                      >
                        Restore
                      </button>
                    </div>
                  )}
                  
                  {(() => {
                    // During build, always show logs view
                    const isBuilding = build && build.status === 'building';
                    const currentViewMode = isBuilding ? 'logs' : (viewMode[config.name] || 'logs');
                    const isMaximized = maximizedService === config.name;
                    const containerClass = isMaximized ? 'h-[calc(100vh-60px)]' : 'max-h-96';
                    
                    // Logs component
                    const LogsView = () => (
                      <div className={`flex flex-col ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
                        {/* Log Filter Controls */}
                        <div className="bg-gray-100 dark:bg-gray-700 p-2 flex gap-2 items-center flex-wrap border-b border-gray-200 dark:border-gray-600">
                          <input
                            type="text"
                            placeholder="Search logs..."
                            value={filter.search}
                            onChange={(e) => setLogFilters({
                              ...logFilters,
                              [config.name]: { ...filter, search: e.target.value }
                            })}
                            className="flex-1 min-w-[200px] px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
                          />
                          <select
                            value={filter.type}
                            onChange={(e) => setLogFilters({
                              ...logFilters,
                              [config.name]: { ...filter, type: e.target.value as 'all' | 'stdout' | 'stderr' }
                            })}
                            className="px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
                            title="Filter log type"
                            aria-label="Filter log type"
                          >
                            <option value="all">All</option>
                            <option value="stdout">Stdout</option>
                            <option value="stderr">Stderr</option>
                          </select>
                          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                            <input
                              type="checkbox"
                              checked={isAutoScroll}
                              onChange={(e) => setAutoScroll({
                                ...autoScroll,
                                [config.name]: e.target.checked
                              })}
                              className="rounded border-gray-300 dark:border-gray-600"
                            />
                            <span>Auto-scroll</span>
                          </label>
                          <span className="text-sm text-gray-600 dark:text-gray-400">
                            {serviceLogs.length} / {logs[config.name]?.length || 0} lines
                          </span>
                        </div>
                        
                        {/* Log Display */}
                        <div className={`bg-black text-green-400 font-mono text-xs p-4 overflow-y-auto flex-1 ${isMaximized ? 'h-full' : ''}`}>
                          {serviceLogs.length === 0 ? (
                            <div className="text-gray-500">
                              {logs[config.name]?.length === 0 
                                ? 'No logs yet...' 
                                : 'No logs match the current filter'}
                            </div>
                          ) : (
                            <>
                              {serviceLogs.map((log, index) => (
                                <div
                                  key={index}
                                  className={log.type === 'stderr' ? 'text-red-400' : 'text-green-400'}
                                >
                                  <span className="text-gray-500">
                                    [{new Date(log.timestamp).toLocaleTimeString()}] [{log.service}]
                                  </span>{' '}
                                  {log.message}
                                </div>
                              ))}
                              <div ref={logEndRef} />
                            </>
                          )}
                        </div>
                      </div>
                    );
                    
                    // Webview component
                    const WebviewView = () => {
                      const hasError = webviewErrors[config.name];
                      const isHttps = config.url?.startsWith('https://');
                      
                      // For HTTPS URLs, show a button to open in Tauri window instead of iframe
                      // Tauri windows can handle self-signed certificates better
                      if (isHttps) {
                        return (
                          <div className={`flex flex-col items-center justify-center h-full p-8 bg-gray-50 dark:bg-gray-900 text-center ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
                            <div className="max-w-md">
                              <div className="text-blue-500 text-5xl mb-4">🔒</div>
                              <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
                                Open {config.displayName} in Secure Window
                              </h3>
                              <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
                                This service uses HTTPS with a self-signed certificate. Click the button below to open it in a Tauri window where you can accept the certificate.
                              </p>
                              <div className="flex flex-col gap-3">
                                <button
                                  onClick={() => openInTauriWindow(config.url!, config.displayName)}
                                  className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 font-medium text-base shadow-lg transition-colors"
                                >
                                  🪟 Open in Tauri Window
                                </button>
                                <button
                                  onClick={() => openInBrowser(config.url!)}
                                  className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 text-sm transition-colors"
                                >
                                  Or open in external browser
                                </button>
                              </div>
                            </div>
                          </div>
                        );
                      }
                      
                      // For HTTP URLs, use iframe as normal
                      return (
                        <div className={`flex flex-col ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
                          {hasError ? (
                            <div className="flex flex-col items-center justify-center h-full p-8 bg-gray-50 dark:bg-gray-900 text-center">
                              <div className="text-red-500 text-4xl mb-4">⚠️</div>
                              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                                Unable to connect to {config.displayName}
                              </h3>
                              <p className="text-sm text-gray-600 dark:text-gray-400 mb-4 max-w-md">
                                The webview cannot connect to {config.url}. This might be due to:
                              </p>
                              <ul className="text-sm text-gray-600 dark:text-gray-400 mb-6 text-left max-w-md list-disc list-inside">
                                <li>Service not fully started yet</li>
                                <li>CORS or security restrictions</li>
                              </ul>
                              <div className="flex gap-2">
                                <button
                                  onClick={() => {
                                    setWebviewErrors((prev) => ({ ...prev, [config.name]: false }));
                                  }}
                                  className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
                                >
                                  Retry
                                </button>
                                <button
                                  onClick={() => openInTauriWindow(config.url!, config.displayName)}
                                  className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
                                >
                                  Open in Tauri Window
                                </button>
                              </div>
                            </div>
                          ) : (
                            <iframe
                              src={config.url}
                              className="w-full flex-1 border-0 min-h-0"
                              title={`${config.displayName} Webview`}
                              sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
                              onError={() => {
                                setWebviewErrors((prev) => ({ ...prev, [config.name]: true }));
                              }}
                            />
                          )}
                        </div>
                      );
                    };
                    
                    // Render based on view mode
                    // During build, only show logs (webview not available yet)
                    if (isBuilding) {
                      return <LogsView />;
                    } else if (currentViewMode === 'logs') {
                      return <LogsView />;
                    } else if (currentViewMode === 'webview') {
                      // Only show webview if service is running
                      if (isRunning && config.url) {
                        return <WebviewView />;
                      } else {
                        return <LogsView />;
                      }
                    } else if (currentViewMode === 'split') {
                      // Only show split if service is running
                      if (isRunning && config.url) {
                        return (
                          <div className={`flex flex-1 ${isMaximized ? 'h-full min-h-0' : containerClass}`}>
                            <div className="flex-1 border-r border-gray-200 dark:border-gray-700 min-w-0 flex flex-col">
                              <LogsView />
                            </div>
                            <div className="flex-1 min-w-0 flex flex-col">
                              <WebviewView />
                            </div>
                          </div>
                        );
                      } else {
                        return <LogsView />;
                      }
                    }
                    return <LogsView />;
                  })()}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default ServiceManager;
