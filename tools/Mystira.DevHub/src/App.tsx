import { useEffect, useState } from 'react';
import './App.css';
import CosmosExplorer from './components/CosmosExplorer';
import Dashboard from './components/Dashboard';
import InfrastructurePanel from './components/InfrastructurePanel';
import MigrationManager from './components/MigrationManager';
import ServiceManager from './components/ServiceManager';
import { Sidebar, type View } from './components/Sidebar';
import { getServiceConfigs } from './components/services';
import { EnvironmentBanner } from './components/services/EnvironmentBanner';
import { useEnvironmentManagement } from './components/services/hooks/useEnvironmentManagement';
import { useDarkMode } from './hooks/useDarkMode';

function App() {
  const [currentView, setCurrentView] = useState<View>('services');
  const [sidebarPosition, setSidebarPosition] = useState<'left' | 'right'>('left');
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const { isDark, toggleDarkMode } = useDarkMode();
  
  // Environment status for header (only when on services view)
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    const saved = localStorage.getItem('serviceEnvironments');
    return saved ? JSON.parse(saved) : {};
  });
  const { environmentStatus, getEnvironmentUrls } = useEnvironmentManagement();
  
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
  
  const serviceConfigs = currentView === 'services' ? getServiceConfigs({}, serviceEnvironments, getEnvironmentUrls) : [];
  
  const getEnvironmentInfo = (serviceName: string) => {
    const env = serviceEnvironments[serviceName] || 'local';
    const config = serviceConfigs.find((c) => c.name === serviceName);
    return { 
      environment: env, 
      url: config?.url || '', 
    };
  };
  
  const handleResetAll = () => {
    if (window.confirm('Switch all services to Local environment?\n\nThis will disconnect from deployed environments.')) {
      const updated: Record<string, 'local' | 'dev' | 'prod'> = { ...serviceEnvironments };
      serviceConfigs.forEach((config) => {
        if (serviceEnvironments[config.name] && serviceEnvironments[config.name] !== 'local') {
          updated[config.name] = 'local';
        }
      });
      setServiceEnvironments(updated);
      localStorage.setItem('serviceEnvironments', JSON.stringify(updated));
    }
  };

  return (
    <div className="h-screen bg-gray-50 dark:bg-gray-900 flex">
      {/* Sidebar */}
      <Sidebar
        currentView={currentView}
        onViewChange={setCurrentView}
        onPositionChange={setSidebarPosition}
        onCollapsedChange={setSidebarCollapsed}
      />

      {/* Main Content */}
      <main
        className={`flex-1 overflow-auto transition-all duration-300 ${
          sidebarPosition === 'left' 
            ? (sidebarCollapsed ? 'ml-12' : 'ml-56')
            : (sidebarCollapsed ? 'mr-12' : 'mr-56')
        }`}
      >
        {/* Top Bar */}
        <div className="sticky top-0 z-30 bg-gray-100 dark:bg-gray-800 border-b border-gray-300 dark:border-gray-600 px-4 py-2 flex items-center justify-between gap-4">
          <div className="flex items-center gap-4 flex-1 min-w-0">
            <h1 className="text-lg font-bold text-gray-900 dark:text-white font-mono">MYSTIRA DEVHUB</h1>
            <span className="text-xs text-gray-500 dark:text-gray-400">Development Operations</span>
            {currentView === 'services' && serviceConfigs.length > 0 && (
              <div className="flex-1 flex items-center justify-center min-w-0">
                <EnvironmentBanner
                  serviceConfigs={serviceConfigs}
                  serviceEnvironments={serviceEnvironments}
                  environmentStatus={environmentStatus}
                  getEnvironmentInfo={getEnvironmentInfo}
                  onResetAll={handleResetAll}
                />
              </div>
            )}
          </div>
          <button
            onClick={toggleDarkMode}
            className="p-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-300 flex-shrink-0"
            title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {isDark ? '☀️' : '🌙'}
          </button>
        </div>

        {/* Content Area */}
        <div className="p-4">
          {currentView === 'services' && <ServiceManager />}
          {currentView === 'dashboard' && <Dashboard onNavigate={handleNavigate} />}
          {currentView === 'cosmos' && <CosmosExplorer />}
          {currentView === 'migration' && <MigrationManager />}
          {currentView === 'infrastructure' && <InfrastructurePanel />}
        </div>
      </main>
    </div>
  );
}

export default App;
