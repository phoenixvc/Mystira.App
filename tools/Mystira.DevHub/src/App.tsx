import { useEffect, useMemo, useState } from 'react';
import './App.css';
import CosmosExplorer from './components/CosmosExplorer';
import Dashboard from './components/Dashboard';
import InfrastructurePanel from './components/InfrastructurePanel';
import MigrationManager from './components/MigrationManager';
import ServiceManager from './components/ServiceManager';
import { VSCodeLayout, type BottomPanelTab } from './components/VSCodeLayout';
import { getServiceConfigs } from './components/services';
import { LogsViewer } from './components/services/LogsViewer';
import { useEnvironmentManagement } from './components/services/hooks/useEnvironmentManagement';
import type { LogFilter, ServiceLog } from './components/services/types';
import { useDarkMode } from './hooks/useDarkMode';

type View = 'services' | 'dashboard' | 'cosmos' | 'migration' | 'infrastructure';

// Activity bar items for main navigation
const ACTIVITY_BAR_ITEMS = [
  { id: 'services', icon: '⚡', title: 'Services' },
  { id: 'dashboard', icon: '📊', title: 'Dashboard' },
  { id: 'cosmos', icon: '🔮', title: 'Cosmos Explorer' },
  { id: 'migration', icon: '🔄', title: 'Migration Manager' },
  { id: 'infrastructure', icon: '☁️', title: 'Infrastructure' },
];

function App() {
  const [currentView, setCurrentView] = useState<View>('services');
  const { isDark, toggleDarkMode } = useDarkMode();

  // Global logs state for bottom panel
  const [globalLogs, setGlobalLogs] = useState<Array<{ timestamp: Date; message: string; type: 'info' | 'error' | 'warn' }>>([]);
  
  // Deployment logs state (separate from global logs for infrastructure deployments)
  const [deploymentLogs, setDeploymentLogs] = useState<string | null>(null);
  
  // LogsViewer state
  const [logFilter, setLogFilter] = useState<LogFilter>({
    search: '',
    type: 'all',
    source: 'all',
    severity: 'all',
  });
  const [isAutoScroll, setIsAutoScroll] = useState(true);
  
  // Global problems state for bottom panel
  const [problems, setProblems] = useState<Array<{ 
    id: string; 
    timestamp: Date; 
    severity: 'error' | 'warning' | 'info'; 
    message: string; 
    source?: string; 
    details?: string;
  }>>([]);

  // Environment status for header (only when on services view)
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    const saved = localStorage.getItem('serviceEnvironments');
    return saved ? JSON.parse(saved) : {};
  });
  const { getEnvironmentUrls } = useEnvironmentManagement();

  // Listen for environment changes
  useEffect(() => {
    const handleStorageChange = () => {
      const saved = localStorage.getItem('serviceEnvironments');
      if (saved) {
        setServiceEnvironments(JSON.parse(saved));
      }
    };
    window.addEventListener('storage', handleStorageChange);
    // Also check periodically for same-tab updates
    const interval = setInterval(handleStorageChange, 1000);
    return () => {
      window.removeEventListener('storage', handleStorageChange);
      clearInterval(interval);
    };
  }, []);

  const handleNavigate = (view: string) => {
    setCurrentView(view as View);
  };

  // Listen for infrastructure navigation requests
  useEffect(() => {
    const handleNavigateToInfrastructure = () => {
      setCurrentView('infrastructure');
    };

    window.addEventListener('navigate-to-infrastructure', handleNavigateToInfrastructure);
    return () => {
      window.removeEventListener('navigate-to-infrastructure', handleNavigateToInfrastructure);
    };
  }, []);

  // Listen for global log events
  useEffect(() => {
    const handleGlobalLog = (event: CustomEvent<{ message: string; type: 'info' | 'error' | 'warn' }>) => {
      setGlobalLogs(prev => [...prev.slice(-500), { timestamp: new Date(), ...event.detail }]);
    };

    window.addEventListener('global-log' as any, handleGlobalLog);
    return () => {
      window.removeEventListener('global-log' as any, handleGlobalLog);
    };
  }, []);

  // Listen for deployment logs
  useEffect(() => {
    const handleDeploymentLogs = (event: CustomEvent<{ logs: string }>) => {
      setDeploymentLogs(event.detail.logs);
    };

    window.addEventListener('deployment-logs' as any, handleDeploymentLogs);
    return () => {
      window.removeEventListener('deployment-logs' as any, handleDeploymentLogs);
    };
  }, []);

  // Listen for infrastructure problems
  useEffect(() => {
    const handleInfrastructureProblem = (event: CustomEvent<{ 
      severity: 'error' | 'warning' | 'info'; 
      message: string; 
      source?: string; 
      details?: string;
      clear?: boolean;
    }>) => {
      if (event.detail.clear) {
        setProblems([]);
      } else {
        const problem = {
          id: `problem-${Date.now()}-${Math.random().toString(36).slice(2)}`,
          timestamp: new Date(),
          severity: event.detail.severity,
          message: event.detail.message,
          source: event.detail.source || 'Infrastructure',
          details: event.detail.details,
        };
        setProblems(prev => {
          // Remove duplicates with same message
          const filtered = prev.filter(p => p.message !== problem.message);
          return [...filtered, problem].slice(-100); // Keep last 100 problems
        });
      }
    };

    window.addEventListener('infrastructure-problem' as any, handleInfrastructureProblem);
    return () => {
      window.removeEventListener('infrastructure-problem' as any, handleInfrastructureProblem);
    };
  }, []);

  const serviceConfigs = getServiceConfigs({}, serviceEnvironments, getEnvironmentUrls);

  // Calculate environment summary for status bar
  const getEnvironmentSummary = () => {
    const localCount = Object.values(serviceEnvironments).filter(e => e === 'local' || !e).length;
    const devCount = Object.values(serviceEnvironments).filter(e => e === 'dev').length;
    const prodCount = Object.values(serviceEnvironments).filter(e => e === 'prod').length;

    const parts: string[] = [];
    if (localCount > 0) parts.push(`${localCount} Local`);
    if (devCount > 0) parts.push(`${devCount} Dev`);
    if (prodCount > 0) parts.push(`${prodCount} Prod`);

    return parts.length > 0 ? parts.join(' | ') : 'All Local';
  };

  // Render the main content based on current view
  const renderContent = () => {
    switch (currentView) {
      case 'services':
        return <ServiceManager />;
      case 'dashboard':
        return <Dashboard onNavigate={handleNavigate} />;
      case 'cosmos':
        return <CosmosExplorer />;
      case 'migration':
        return <MigrationManager />;
      case 'infrastructure':
        return <InfrastructurePanel />;
      default:
        return <ServiceManager />;
    }
  };

  // Convert logs and problems to ServiceLog format for LogsViewer
  const allLogs = useMemo<ServiceLog[]>(() => {
    const logs: ServiceLog[] = [];
    
    // Add deployment logs
    if (deploymentLogs) {
      const lines = deploymentLogs.split('\n');
      lines.forEach((line, index) => {
        if (line.trim()) {
          const isError = line.toLowerCase().includes('error') || line.toLowerCase().includes('failed');
          logs.push({
            service: 'Infrastructure',
            type: isError ? 'stderr' : 'stdout',
            source: 'run',
            message: line,
            timestamp: Date.now() - (lines.length - index) * 1000, // Stagger timestamps slightly
          });
        }
      });
    }
    
    // Add global logs
    globalLogs.forEach((log) => {
      logs.push({
        service: 'System',
        type: log.type === 'error' ? 'stderr' : 'stdout',
        source: 'run',
        message: log.message,
        timestamp: log.timestamp.getTime(),
      });
    });
    
    // Add problems as logs (errors/warnings)
    problems.forEach((problem) => {
      logs.push({
        service: problem.source || 'Infrastructure',
        type: problem.severity === 'error' ? 'stderr' : 'stdout',
        source: 'run',
        message: `[${problem.severity.toUpperCase()}] ${problem.message}${problem.details ? '\n' + problem.details : ''}`,
        timestamp: problem.timestamp.getTime(),
      });
    });
    
    return logs;
  }, [globalLogs, deploymentLogs, problems]);

  // Filter logs based on current filter
  const filteredLogs = useMemo<ServiceLog[]>(() => {
    let filtered = allLogs;

    // Filter by search
    if (logFilter.search) {
      const searchLower = logFilter.search.toLowerCase();
      filtered = filtered.filter(log => 
        log.message.toLowerCase().includes(searchLower) ||
        log.service.toLowerCase().includes(searchLower)
      );
    }

    // Filter by type
    if (logFilter.type !== 'all') {
      filtered = filtered.filter(log => log.type === logFilter.type);
    }

    // Filter by source
    if (logFilter.source && logFilter.source !== 'all') {
      filtered = filtered.filter(log => log.source === logFilter.source);
    }

    // Filter by severity
    if (logFilter.severity && logFilter.severity !== 'all') {
      filtered = filtered.filter(log => {
        const msgLower = log.message.toLowerCase();
        if (logFilter.severity === 'errors') {
          return log.type === 'stderr' || msgLower.includes('error') || msgLower.includes('failed');
        } else if (logFilter.severity === 'warnings') {
          return msgLower.includes('warning') || msgLower.includes('warn');
        }
        return true;
      });
    }

    return filtered;
  }, [allLogs, logFilter]);

  // Build bottom panel tabs
  const bottomPanelTabs: BottomPanelTab[] = [
    {
      id: 'output',
      title: 'Output',
      icon: '📋',
      badge: (allLogs.length > 0 || problems.length > 0) ? (allLogs.length + problems.length) : undefined,
      content: (
        <LogsViewer
          serviceName="Output"
          logs={allLogs}
          filteredLogs={filteredLogs}
          filter={logFilter}
          isAutoScroll={isAutoScroll}
          isMaximized={true}
          containerClass="h-full"
          onFilterChange={setLogFilter}
          onAutoScrollChange={setIsAutoScroll}
          onClearLogs={() => {
            setGlobalLogs([]);
            setDeploymentLogs(null);
          }}
        />
      ),
    },
    {
      id: 'terminal',
      title: 'Terminal',
      icon: '▸',
      content: (
        <div className="h-full overflow-auto p-2 font-mono text-xs bg-gray-900 text-gray-300">
          <div className="text-gray-500 italic">Terminal not available in this context</div>
        </div>
      ),
    },
  ];

  // Primary sidebar content - contextual based on view
  const renderPrimarySidebar = () => {
    return (
      <div className="text-xs text-gray-300">
        {/* View-specific sidebar content */}
        <div className="p-3 border-b border-gray-700">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-lg">{ACTIVITY_BAR_ITEMS.find(a => a.id === currentView)?.icon}</span>
            <span className="font-semibold text-white uppercase tracking-wide">
              {ACTIVITY_BAR_ITEMS.find(a => a.id === currentView)?.title}
            </span>
          </div>
          <p className="text-gray-400 text-[10px]">
            {currentView === 'services' && 'Manage local and deployed services'}
            {currentView === 'dashboard' && 'Overview and quick actions'}
            {currentView === 'cosmos' && 'Explore Azure Cosmos DB'}
            {currentView === 'migration' && 'Database migration tools'}
            {currentView === 'infrastructure' && 'Deploy and manage Azure resources'}
          </p>
        </div>

        {/* Quick actions */}
        <div className="p-3">
          <div className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Quick Actions</div>
          <div className="space-y-1">
            {currentView === 'services' && (
              <>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>▶</span> Start All Services
                </button>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>⏹</span> Stop All Services
                </button>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>🔄</span> Refresh Status
                </button>
              </>
            )}
            {currentView === 'infrastructure' && (
              <>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>☁️</span> Deploy to Azure
                </button>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>🔍</span> View Resources
                </button>
              </>
            )}
            {currentView === 'cosmos' && (
              <>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>🔌</span> Connect Database
                </button>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>📝</span> New Query
                </button>
              </>
            )}
            {currentView === 'migration' && (
              <>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>➕</span> New Migration
                </button>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>▶</span> Run Pending
                </button>
              </>
            )}
            {currentView === 'dashboard' && (
              <>
                <button className="w-full text-left px-2 py-1 rounded hover:bg-gray-700 text-gray-300 flex items-center gap-2">
                  <span>🔄</span> Refresh Data
                </button>
              </>
            )}
          </div>
        </div>

        {/* Service list (only on services view) */}
        {currentView === 'services' && serviceConfigs.length > 0 && (
          <div className="p-3 border-t border-gray-700">
            <div className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider mb-2">Services</div>
            <div className="space-y-0.5">
              {serviceConfigs.map((config) => {
                const env = serviceEnvironments[config.name] || 'local';
                return (
                  <div
                    key={config.name}
                    className="flex items-center justify-between px-2 py-1 rounded hover:bg-gray-700"
                  >
                    <span className="truncate">{config.displayName || config.name}</span>
                    <span className={`text-[9px] px-1.5 py-0.5 rounded ${
                      env === 'prod' ? 'bg-red-900/50 text-red-300' :
                      env === 'dev' ? 'bg-blue-900/50 text-blue-300' :
                      'bg-gray-700 text-gray-400'
                    }`}>
                      {env.toUpperCase()}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>
    );
  };

  return (
    <VSCodeLayout
      activityBarItems={ACTIVITY_BAR_ITEMS}
      activeActivityId={currentView}
      onActivityChange={(id) => setCurrentView(id as View)}
      primarySidebar={renderPrimarySidebar()}
      primarySidebarTitle={ACTIVITY_BAR_ITEMS.find(a => a.id === currentView)?.title}
      bottomPanelTabs={bottomPanelTabs}
      defaultBottomTab="output"
      statusBarLeft={
        <>
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 rounded-full bg-green-500"></span>
            <span>MYSTIRA DEVHUB</span>
          </span>
          <span className="text-blue-200">{getEnvironmentSummary()}</span>
        </>
      }
      statusBarRight={
        <>
          <button
            onClick={toggleDarkMode}
            className="hover:bg-blue-500 px-1 rounded transition-colors"
            title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {isDark ? '☀️' : '🌙'}
          </button>
          <span>v1.0.0</span>
        </>
      }
      storageKey="devhubLayout"
    >
      {renderContent()}
    </VSCodeLayout>
  );
}

export default App;
