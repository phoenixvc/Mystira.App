import { open as openDialog } from '@tauri-apps/api/dialog';
import { listen } from '@tauri-apps/api/event';
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

interface ServiceLog {
  service: string;
  type: 'stdout' | 'stderr';
  message: string;
  timestamp: number;
}

function ServiceManager() {
  const [services, setServices] = useState<ServiceStatus[]>([]);
  const [loading, setLoading] = useState<Record<string, boolean>>({});
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [currentBranch, setCurrentBranch] = useState<string>('');
  const [useCurrentBranch, setUseCurrentBranch] = useState<boolean>(false);
  const [logs, setLogs] = useState<Record<string, ServiceLog[]>>({});
  const [selectedService, setSelectedService] = useState<string | null>(null);
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});
  const [toasts, setToasts] = useState<Toast[]>([]);
  const [logFilters, setLogFilters] = useState<Record<string, {
    search: string;
    type: 'all' | 'stdout' | 'stderr';
  }>>({});
  const [autoScroll, setAutoScroll] = useState<Record<string, boolean>>({});
  const logEndRef = useRef<HTMLDivElement>(null);
  const { showToast } = useToast();

  const addToast = (message: string, type: Toast['type'] = 'info', duration: number = 5000) => {
    const toast = showToast(message, type, duration);
    setToasts((prev) => [...prev, toast]);
  };

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  useEffect(() => {
    // Get repository root from Tauri - use current path as default
    const loadRepoRoot = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: root });
          setCurrentBranch(branch);
        } catch (error) {
          console.error('Failed to get current branch:', error);
        }
      } catch (error) {
        console.error('Failed to get repo root:', error);
        setRepoRoot('C:\\Users\\smitj\\repos\\Mystira.App');
      }
    };
    
    loadRepoRoot();
    refreshServices();
    const interval = setInterval(refreshServices, 2000);
    return () => clearInterval(interval);
  }, []);

  // Listen for service logs
  useEffect(() => {
    const setupLogListener = async () => {
      const unlisten = await listen<ServiceLog>('service-log', (event) => {
        const log = { 
          ...event.payload, 
          timestamp: (event.payload as any).timestamp || Date.now() 
        };
        setLogs((prevLogs) => {
          const serviceLogs = prevLogs[log.service] || [];
          return {
            ...prevLogs,
            [log.service]: [...serviceLogs, log].slice(-1000), // Keep last 1000 lines
          };
        });
      });
      return unlisten;
    };

    setupLogListener().then((unlisten) => {
      return () => {
        unlisten();
      };
    });
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
    try {
      const config = serviceConfigs.find(s => s.name === serviceName);
      
      // Check for port conflicts before starting
      if (config?.port) {
        const available = await invoke<boolean>('check_port_available', { port: config.port });
        if (!available) {
          addToast(`Port ${config.port} is already in use!`, 'warning', 7000);
          setLoading({ ...loading, [serviceName]: false });
          return;
        }
      }
      
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;
      
      await invoke<ServiceStatus>('start_service', {
        serviceName,
        repoRoot: rootToUse,
      });
      
      setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
      setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
      setSelectedService(serviceName);
      setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
      await refreshServices();
      addToast(`${config?.displayName || serviceName} started successfully`, 'success');
    } catch (error) {
      addToast(`Failed to start ${serviceName}: ${error}`, 'error');
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

  const openInWebview = async (url: string, title: string) => {
    try {
      await invoke('create_webview_window', { url, title });
      addToast(`Opened ${title} in webview`, 'success');
    } catch (error) {
      console.error('Failed to create webview window:', error);
      await open(url);
    }
  };

  const toggleLogs = (serviceName: string) => {
    setShowLogs((prev) => ({ ...prev, [serviceName]: !prev[serviceName] }));
    if (!showLogs[serviceName]) {
      setSelectedService(serviceName);
      setAutoScroll((prev) => ({ ...prev, [serviceName]: true }));
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

    setLoading({ ...loading, ...Object.fromEntries(servicesToStart.map(s => [s.name, true])) });
    
    try {
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}`
        : repoRoot;

      // Check for port conflicts first
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

      const startPromises = servicesToStart.map(service => 
        invoke<ServiceStatus>('start_service', {
          serviceName: service.name,
          repoRoot: rootToUse,
        }).then(() => {
          setLogs((prevLogs) => ({ ...prevLogs, [service.name]: [] }));
          setShowLogs((prev) => ({ ...prev, [service.name]: true }));
          setAutoScroll((prev) => ({ ...prev, [service.name]: true }));
        }).catch(error => {
          console.error(`Failed to start ${service.name}:`, error);
          return { service: service.name, error };
        })
      );

      const results = await Promise.allSettled(startPromises);
      const failures = results
        .map((result, index) => result.status === 'rejected' ? servicesToStart[index].name : null)
        .filter(Boolean);

      if (failures.length > 0) {
        addToast(`Failed to start: ${failures.join(', ')}`, 'error');
      } else {
        addToast(`Started ${servicesToStart.length} service(s)`, 'success');
      }

      await refreshServices();
    } catch (error) {
      addToast(`Failed to start services: ${error}`, 'error');
    } finally {
      setLoading({ ...loading, ...Object.fromEntries(servicesToStart.map(s => [s.name, false])) });
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

  const serviceConfigs = [
    { name: 'api', displayName: 'API', port: 7096, url: 'https://localhost:7096/swagger' },
    { name: 'admin-api', displayName: 'Admin API', port: 7096, url: 'https://localhost:7096/admin' },
    { name: 'pwa', displayName: 'PWA', port: 7000, url: 'http://localhost:7000' },
  ];

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
          <h1 className="text-3xl font-bold">Service Manager</h1>
          <p className="text-sm text-gray-500 mt-1">
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
      <div className="mb-6 p-4 bg-gray-50 rounded-lg border">
        <h2 className="text-lg font-semibold mb-3">Repository Configuration</h2>
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <label className="font-medium">Repository Root:</label>
            <input
              type="text"
              value={repoRoot}
              onChange={(e) => setRepoRoot(e.target.value)}
              className="flex-1 px-3 py-2 border rounded"
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
              <label className="font-medium">Current Branch:</label>
              <span className="px-3 py-1 bg-blue-100 text-blue-800 rounded">{currentBranch}</span>
              <label className="flex items-center gap-2">
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
          const serviceLogs = getServiceLogs(config.name);
          const showServiceLogs = showLogs[config.name] || false;
          const filter = logFilters[config.name] || { search: '', type: 'all' };
          const isAutoScroll = autoScroll[config.name] !== false;

          return (
            <div
              key={config.name}
              className="border rounded-lg bg-white shadow-sm"
            >
              <div className="p-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <h3 className="text-xl font-semibold">{config.displayName}</h3>
                      <span
                        className={`px-2 py-1 rounded text-sm ${
                          isRunning
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-800'
                        }`}
                      >
                        {isRunning ? 'Running' : 'Stopped'}
                      </span>
                      {isRunning && getHealthIndicator(status?.health)}
                      {status?.portConflict && (
                        <span className="px-2 py-1 rounded text-sm bg-yellow-100 text-yellow-800" title="Port conflict detected">
                          ⚠ Port {config.port} in use
                        </span>
                      )}
                      {isRunning && config.port && (
                        <span className="text-sm text-gray-600">Port: {config.port}</span>
                      )}
                    </div>
                    {isRunning && config.url && (
                      <div className="mt-2 flex gap-2 flex-wrap">
                        <button
                          onClick={() => openInBrowser(config.url!)}
                          className="px-3 py-1 bg-blue-500 text-white rounded text-sm hover:bg-blue-600"
                        >
                          Open in External Browser
                        </button>
                        <button
                          onClick={() => openInWebview(config.url!, config.displayName)}
                          className="px-3 py-1 bg-purple-500 text-white rounded text-sm hover:bg-purple-600"
                        >
                          Open in Webview
                        </button>
                        <button
                          onClick={() => toggleLogs(config.name)}
                          className="px-3 py-1 bg-gray-500 text-white rounded text-sm hover:bg-gray-600"
                        >
                          {showServiceLogs ? 'Hide' : 'Show'} Logs
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
                        {isLoading ? 'Starting...' : 'Start'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
              
              {/* Console Output with Filtering */}
              {showServiceLogs && (
                <div className="border-t">
                  {/* Log Filter Controls */}
                  <div className="bg-gray-100 p-2 flex gap-2 items-center flex-wrap">
                    <input
                      type="text"
                      placeholder="Search logs..."
                      value={filter.search}
                      onChange={(e) => setLogFilters({
                        ...logFilters,
                        [config.name]: { ...filter, search: e.target.value }
                      })}
                      className="flex-1 min-w-[200px] px-2 py-1 border rounded text-sm"
                    />
                    <select
                      value={filter.type}
                      onChange={(e) => setLogFilters({
                        ...logFilters,
                        [config.name]: { ...filter, type: e.target.value as 'all' | 'stdout' | 'stderr' }
                      })}
                      className="px-2 py-1 border rounded text-sm"
                      title="Filter log type"
                      aria-label="Filter log type"
                    >
                      <option value="all">All</option>
                      <option value="stdout">Stdout</option>
                      <option value="stderr">Stderr</option>
                    </select>
                    <label className="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        checked={isAutoScroll}
                        onChange={(e) => setAutoScroll({
                          ...autoScroll,
                          [config.name]: e.target.checked
                        })}
                      />
                      <span>Auto-scroll</span>
                    </label>
                    <span className="text-sm text-gray-600">
                      {serviceLogs.length} / {logs[config.name]?.length || 0} lines
                    </span>
                  </div>
                  
                  {/* Log Display */}
                  <div className="bg-black text-green-400 font-mono text-xs p-4 max-h-96 overflow-y-auto">
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
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default ServiceManager;
