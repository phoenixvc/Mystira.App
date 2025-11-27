import { useEffect, useState } from 'react';
import './App.css';
import CosmosExplorer from './components/CosmosExplorer';
import Dashboard from './components/Dashboard';
import InfrastructurePanel from './components/InfrastructurePanel';
import MigrationManager from './components/MigrationManager';
import ServiceManager from './components/ServiceManager';
import { VSCodeLayout, type BottomPanelTab } from './components/VSCodeLayout';
import { getServiceConfigs } from './components/services';
import { useEnvironmentManagement } from './components/services/hooks/useEnvironmentManagement';
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

  // Build bottom panel tabs
  const bottomPanelTabs: BottomPanelTab[] = [
    {
      id: 'output',
      title: 'Output',
      icon: '📋',
      badge: globalLogs.length > 0 ? globalLogs.length : undefined,
      content: (
        <div className="h-full overflow-auto p-2 font-mono text-xs bg-gray-900 text-gray-300">
          {globalLogs.length === 0 ? (
            <div className="text-gray-500 italic">No output messages</div>
          ) : (
            globalLogs.map((log, i) => (
              <div
                key={i}
                className={`py-0.5 ${
                  log.type === 'error' ? 'text-red-400' :
                  log.type === 'warn' ? 'text-yellow-400' :
                  'text-gray-300'
                }`}
              >
                <span className="text-gray-500">[{log.timestamp.toLocaleTimeString()}]</span>{' '}
                {log.message}
              </div>
            ))
          )}
        </div>
      ),
    },
    {
      id: 'problems',
      title: 'Problems',
      icon: '⚠️',
      content: (
        <div className="h-full overflow-auto p-2 text-xs bg-gray-900 text-gray-300">
          <div className="text-gray-500 italic">No problems detected</div>
        </div>
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
