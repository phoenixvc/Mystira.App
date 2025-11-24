import { open as openDialog } from '@tauri-apps/api/dialog';
import { listen, UnlistenFn } from '@tauri-apps/api/event';
import { open } from '@tauri-apps/api/shell';
import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import { useKeyboardShortcuts } from '../hooks/useKeyboardShortcut';
import {
  checkEnvironmentContext,
  EnvironmentBanner,
  EnvironmentPresetSelector,
  getServiceConfigs,
  ServiceCard,
  useEnvironmentManagement,
  type BuildStatus,
  type EnvironmentPreset,
  type ServiceConfig,
  type ServiceLog,
  type ServiceStatus,
} from './services';
import { Toast, ToastContainer, useToast } from './Toast';

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
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    // Load from localStorage
    const saved = localStorage.getItem('serviceEnvironments');
    return saved ? JSON.parse(saved) : {};
  });
  // Use environment management hook
  const {
    environmentStatus,
    getEnvironmentUrls,
    fetchEnvironmentUrls,
    checkEnvironmentHealth,
  } = useEnvironmentManagement();
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
      // Ensure logs are visible when build fails
      setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
      setViewModeForService(serviceName, 'logs');
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
    const prebuildPromises = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).map((config: ServiceConfig) =>
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
        
        // Fetch environment URLs from Azure
        fetchEnvironmentUrls().then(() => {
          // After fetching URLs, check health of all available environments
          setTimeout(() => {
            getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).forEach((config: ServiceConfig) => {
              const envUrls = getEnvironmentUrls(config.name);
              if (envUrls.dev) {
                checkEnvironmentHealth(config.name, 'dev');
              }
              if (envUrls.prod) {
                checkEnvironmentHealth(config.name, 'prod');
              }
            });
          }, 1000);
        });
      } catch (error) {
        console.error('Failed to get repo root:', error);
        addToast('Warning: Not running in Tauri. Please use the Tauri application window.', 'warning', 5000);
        setRepoRoot('C:\\Users\\smitj\\repos\\Mystira.App');
      }
    };
    
    initialize();
    const interval = setInterval(refreshServices, 2000);
    
    // Check environment health periodically for all non-local environments
    const healthCheckInterval = setInterval(() => {
      // Check all available environments, not just current ones
      const baseConfigs = [
        { name: 'api', displayName: 'API' },
        { name: 'admin-api', displayName: 'Admin API' },
        { name: 'pwa', displayName: 'PWA' },
      ];
      
      baseConfigs.forEach(config => {
        const envUrls = getEnvironmentUrls(config.name);
        // Check dev if available
        if (envUrls.dev) {
          checkEnvironmentHealth(config.name, 'dev');
        }
        // Check prod if available
        if (envUrls.prod) {
          checkEnvironmentHealth(config.name, 'prod');
        }
      });
    }, 30000); // Every 30 seconds
    
    return () => {
      clearInterval(interval);
      clearInterval(healthCheckInterval);
    };
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
      
      // Only update state if something actually changed to prevent unnecessary re-renders
      setServices(prev => {
        // Check if any service status changed
        const hasChanged = enrichedStatuses.length !== prev.length ||
          enrichedStatuses.some(newStatus => {
            const oldStatus = prev.find(s => s.name === newStatus.name);
            if (!oldStatus) return true;
            // Only update if running status, port, URL, health, or portConflict changed
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
    // Check if service is set to a deployed environment
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      const envName = environment.toUpperCase();
      addToast(
        `${serviceName} is set to ${envName} environment. It will connect to the deployed service, not start locally.`,
        'info',
        5000
      );
      // Mark as "running" since it's using deployed service
      setServices(prev => {
        const existing = prev.find(s => s.name === serviceName);
        if (existing) {
          return prev.map(s => s.name === serviceName ? { ...s, running: true } : s);
        }
        const envUrls = getEnvironmentUrls(serviceName);
        const url = environment === 'dev' ? envUrls.dev : envUrls.prod;
        return [...prev, {
          name: serviceName,
          running: true,
          url,
        }];
      });
      return;
    }
    
    setLoading({ ...loading, [serviceName]: true });
    setStatusMessage(prev => ({ ...prev, [serviceName]: 'Preparing...' }));
    
    // Initialize logs BEFORE starting (so we capture build output)
    setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
    setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
    setSelectedService(serviceName);
    setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
    
    try {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
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
    // Check if service is set to a deployed environment
    const environment = serviceEnvironments[serviceName] || 'local';
    if (environment !== 'local') {
      // Just mark as stopped - no process to kill
      setServices(prev => prev.map(s => 
        s.name === serviceName ? { ...s, running: false } : s
      ));
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
      addToast(`${config?.displayName || serviceName} disconnected from ${environment.toUpperCase()} environment`, 'info');
      return;
    }
    
    setLoading({ ...loading, [serviceName]: true });
    try {
      const config = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).find((s: ServiceConfig) => s.name === serviceName);
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

  const openInTauriWindow = async (url: string, title: string) => {
    try {
      await invoke('create_webview_window', { url, title });
      addToast(`Opened ${title} in Tauri window`, 'success');
    } catch (error) {
      console.error('Failed to create Tauri window:', error);
      addToast('Failed to open Tauri window, opening in external browser instead', 'warning');
      // Fallback to external browser
      await openInBrowser(url);
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
    const servicesToStart = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).filter((config: ServiceConfig) => {
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

  // Get service configs using utility function (computed on each render)
  
  // Update port for a service
  const updateServicePort = async (serviceName: string, port: number, persistToFile: boolean = false) => {
    if (port < 1 || port > 65535) {
      addToast('Port must be between 1 and 65535', 'error');
      return;
    }
    
    const configs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
    const config = configs.find((s: ServiceConfig) => s.name === serviceName);
    const displayName = config?.displayName || serviceName;
    
    // Check for conflicts
    const conflictingService = configs.find((c: ServiceConfig) => c.name !== serviceName && c.port === port);
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

  // Switch environment for a service
  const switchServiceEnvironment = async (serviceName: string, environment: 'local' | 'dev' | 'prod') => {
    // Check environment context for warnings
    const contextCheck = checkEnvironmentContext(
      serviceName,
      environment,
      serviceEnvironments,
      getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls)
    );
    
    // Show context warning if needed
    if (contextCheck.shouldWarn) {
      const confirmed = window.confirm(contextCheck.message);
      if (!confirmed) {
        return;
      }
    }
    
    // Show warning for prod (always, even if context check passed)
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
      
      if (!confirmed) {
        return;
      }
    }
    
    // Stop the service if it's running (since we're switching environments)
    const status = getServiceStatus(serviceName);
    if (status?.running) {
      const stopConfirmed = window.confirm(
        `The ${serviceName} service is currently running. It needs to be stopped before switching environments.\n\n` +
        'Would you like to stop it now?'
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
    
    // Check health of the new environment
    if (environment !== 'local') {
      checkEnvironmentHealth(serviceName, environment);
    }
    
    const envName = environment === 'local' ? 'Local' : environment.toUpperCase();
    addToast(`${serviceName} switched to ${envName} environment`, 'success');
  };

  const serviceConfigs = getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls);
  const allRunning = services.length === serviceConfigs.length && services.every((s: ServiceStatus) => s.running);
  const anyRunning = services.some((s: ServiceStatus) => s.running);

  // Get environment display info
  const getEnvironmentInfo = (serviceName: string) => {
    const env = serviceEnvironments[serviceName] || 'local';
    const config = serviceConfigs.find((c: ServiceConfig) => c.name === serviceName);
    const envUrls = getEnvironmentUrls(serviceName);
    
    return {
      environment: env,
      url: config?.url || '',
      hasDev: !!envUrls.dev,
      hasProd: !!envUrls.prod,
    };
  };

  return (
    <div className="p-8">
      <ToastContainer toasts={toasts} onClose={removeToast} />
      
      {/* Very Visible Environment Indicator */}
      <EnvironmentBanner
        serviceConfigs={serviceConfigs}
        serviceEnvironments={serviceEnvironments}
        environmentStatus={environmentStatus}
        getEnvironmentInfo={getEnvironmentInfo}
        onResetAll={() => {
          if (window.confirm('Switch all services to Local environment?\n\nThis will disconnect from deployed environments.')) {
            getServiceConfigs(customPorts, serviceEnvironments, getEnvironmentUrls).forEach((config: ServiceConfig) => {
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
            Keyboard shortcuts: Ctrl+Shift+S (Start All), Ctrl+Shift+X (Stop All), Ctrl+L (Logs), Ctrl+R (Refresh)
          </p>
        </div>
        <div className="flex gap-2 items-center">
          <EnvironmentPresetSelector
            currentEnvironments={serviceEnvironments}
            onApplyPreset={(preset: EnvironmentPreset) => {
              // Check for warnings before applying
              const hasProd = Object.values(preset.environments).includes('prod');
              if (hasProd) {
                const confirmed = window.confirm(
                  '⚠️ WARNING: This preset includes PRODUCTION environments.\n\n' +
                  'Are you sure you want to apply this preset?'
                );
                if (!confirmed) return;
              }
              
              // Apply preset
              setServiceEnvironments(preset.environments);
              localStorage.setItem('serviceEnvironments', JSON.stringify(preset.environments));
              
              // Check health for non-local environments
              Object.entries(preset.environments).forEach(([serviceName, env]) => {
                if (env !== 'local' && (env === 'dev' || env === 'prod')) {
                  checkEnvironmentHealth(serviceName, env as 'dev' | 'prod');
                }
              });
              
              addToast(`Applied preset: ${preset.name}`, 'success');
            }}
            onSaveCurrent={() => {
              // This is handled by the preset selector component
            }}
          />
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

          return (
            <ServiceCard
              key={config.name}
              config={config}
              status={status}
              build={build}
              isRunning={isRunning}
              isLoading={isLoading}
              statusMsg={statusMsg}
              serviceLogs={serviceLogs}
              logs={logs[config.name] || []}
              filter={filter}
              isAutoScroll={isAutoScroll}
              viewMode={viewMode[config.name] || 'logs'}
              isMaximized={maximizedService === config.name}
              webviewError={webviewErrors[config.name] || false}
              currentEnv={serviceEnvironments[config.name] || 'local'}
              envUrls={getEnvironmentUrls(config.name)}
              environmentStatus={environmentStatus[config.name]}
              deploymentInfo={null}
              onStart={() => startService(config.name)}
              onStop={() => stopService(config.name)}
              onPortChange={(port) => updateServicePort(config.name, port, false)}
              onEnvironmentSwitch={(env) => switchServiceEnvironment(config.name, env)}
              onViewModeChange={(mode) => setViewModeForService(config.name, mode)}
              onMaximize={() => toggleMaximize(config.name)}
              onOpenInBrowser={openInBrowser}
              onOpenInTauriWindow={openInTauriWindow}
              onClearLogs={() => clearLogs(config.name)}
              onFilterChange={(newFilter) => {
                setLogFilters({ ...logFilters, [config.name]: newFilter });
              }}
              onAutoScrollChange={(enabled) => {
                setAutoScroll({ ...autoScroll, [config.name]: enabled });
              }}
              onWebviewRetry={() => {
                setWebviewErrors((prev) => ({ ...prev, [config.name]: false }));
              }}
              onWebviewError={() => {
                setWebviewErrors((prev) => ({ ...prev, [config.name]: true }));
              }}
            />
          );
        })}
      </div>
    </div>
  );
}

export default ServiceManager;
