import { useState, useEffect, useRef } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import { open } from '@tauri-apps/api/shell';
import { listen } from '@tauri-apps/api/event';
import { open as openDialog } from '@tauri-apps/api/dialog';

interface ServiceStatus {
  name: string;
  running: boolean;
  port?: number;
  url?: string;
}

interface ServiceLog {
  service: string;
  type: 'stdout' | 'stderr';
  message: string;
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
  const logEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Get repository root from Tauri - use current path as default
    const loadRepoRoot = async () => {
      try {
        // First try to get the current working directory (where DevHub is running)
        // This will be the repo root if running from the repo root
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
        // Try to get current branch
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: root });
          setCurrentBranch(branch);
        } catch (error) {
          console.error('Failed to get current branch:', error);
        }
      } catch (error) {
        console.error('Failed to get repo root:', error);
        // Try to get current directory as fallback
        try {
          // The get_repo_root function should handle finding the repo root
          // If it fails, we'll use a reasonable default
          setRepoRoot('C:\\Users\\smitj\\repos\\Mystira.App');
        } catch (e) {
          console.error('Failed to set default repo root:', e);
        }
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
        const log = event.payload;
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
    if (logEndRef.current && selectedService) {
      logEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [logs, selectedService]);

  const refreshServices = async () => {
    try {
      const statuses = await invoke<ServiceStatus[]>('get_service_status');
      setServices(statuses);
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
        // Try to get current branch for new root
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: selected });
          setCurrentBranch(branch);
        } catch (error) {
          console.error('Failed to get current branch:', error);
          setCurrentBranch('');
        }
      }
    } catch (error) {
      console.error('Failed to pick directory:', error);
    }
  };

  const startService = async (serviceName: string) => {
    setLoading({ ...loading, [serviceName]: true });
    try {
      // Determine which repo root to use
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}` // Assuming branch-based directories
        : repoRoot;
      
      await invoke<ServiceStatus>('start_service', {
        serviceName,
        repoRoot: rootToUse,
      });
      // Clear logs for this service
      setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
      setShowLogs((prev) => ({ ...prev, [serviceName]: true }));
      setSelectedService(serviceName);
      await refreshServices();
    } catch (error) {
      alert(`Failed to start ${serviceName}: ${error}`);
    } finally {
      setLoading({ ...loading, [serviceName]: false });
    }
  };

  const stopService = async (serviceName: string) => {
    setLoading({ ...loading, [serviceName]: true });
    try {
      await invoke('stop_service', { serviceName });
      await refreshServices();
    } catch (error) {
      alert(`Failed to stop ${serviceName}: ${error}`);
    } finally {
      setLoading({ ...loading, [serviceName]: false });
    }
  };

  const openInBrowser = async (url: string) => {
    try {
      await open(url);
    } catch (error) {
      console.error('Failed to open URL:', error);
    }
  };

  const openInWebview = async (url: string, title: string) => {
    try {
      await invoke('create_webview_window', { url, title });
    } catch (error) {
      console.error('Failed to create webview window:', error);
      // Fallback to external browser
      await open(url);
    }
  };

  const toggleLogs = (serviceName: string) => {
    setShowLogs((prev) => ({ ...prev, [serviceName]: !prev[serviceName] }));
    if (!showLogs[serviceName]) {
      setSelectedService(serviceName);
    }
  };

  const clearLogs = (serviceName: string) => {
    setLogs((prevLogs) => ({ ...prevLogs, [serviceName]: [] }));
  };

  const startAllServices = async () => {
    const servicesToStart = serviceConfigs.filter(config => {
      const status = getServiceStatus(config.name);
      return !status?.running;
    });

    if (servicesToStart.length === 0) {
      alert('All services are already running!');
      return;
    }

    setLoading({ ...loading, ...Object.fromEntries(servicesToStart.map(s => [s.name, true])) });
    
    try {
      // Determine which repo root to use
      const rootToUse = useCurrentBranch && currentBranch 
        ? `${repoRoot}\\..\\Mystira.App-${currentBranch}` // Assuming branch-based directories
        : repoRoot;

      // Start all services in parallel
      const startPromises = servicesToStart.map(service => 
        invoke<ServiceStatus>('start_service', {
          serviceName: service.name,
          repoRoot: rootToUse,
        }).then(() => {
          // Clear logs for this service
          setLogs((prevLogs) => ({ ...prevLogs, [service.name]: [] }));
          setShowLogs((prev) => ({ ...prev, [service.name]: true }));
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
        alert(`Failed to start: ${failures.join(', ')}`);
      }

      await refreshServices();
    } catch (error) {
      alert(`Failed to start services: ${error}`);
    } finally {
      setLoading({ ...loading, ...Object.fromEntries(servicesToStart.map(s => [s.name, false])) });
    }
  };

  const stopAllServices = async () => {
    const runningServices = services.filter(s => s.running);
    
    if (runningServices.length === 0) {
      alert('No services are running!');
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
        alert(`Failed to stop: ${failures.join(', ')}`);
      }

      await refreshServices();
    } catch (error) {
      alert(`Failed to stop services: ${error}`);
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
    return logs[serviceName] || [];
  };

  const allRunning = services.length === serviceConfigs.length && services.every(s => s.running);
  const anyRunning = services.some(s => s.running);

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Service Manager</h1>
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
                      {isRunning && config.port && (
                        <span className="text-sm text-gray-600">Port: {config.port}</span>
                      )}
                    </div>
                    {isRunning && config.url && (
                      <div className="mt-2 flex gap-2">
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
                        disabled={isLoading}
                        className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:opacity-50"
                      >
                        {isLoading ? 'Starting...' : 'Start'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
              
              {/* Console Output */}
              {showServiceLogs && (
                <div className="border-t bg-black text-green-400 font-mono text-xs p-4 max-h-96 overflow-y-auto">
                  {serviceLogs.length === 0 ? (
                    <div className="text-gray-500">No logs yet...</div>
                  ) : (
                    <>
                      {serviceLogs.map((log, index) => (
                        <div
                          key={index}
                          className={log.type === 'stderr' ? 'text-red-400' : 'text-green-400'}
                        >
                          <span className="text-gray-500">[{log.service}]</span> {log.message}
                        </div>
                      ))}
                      <div ref={logEndRef} />
                    </>
                  )}
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
