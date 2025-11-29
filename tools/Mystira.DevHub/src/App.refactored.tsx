import { useEffect, useState } from 'react';
import './App.css';
import { useAppBottomPanelTabs } from './components/AppBottomPanel';
import { AppContent } from './components/AppContent';
import { AppSidebar } from './components/AppSidebar';
import { VSCodeLayout } from './components/VSCodeLayout';
import { getServiceConfigs } from './components/services';
import { useEnvironmentManagement } from './components/services/hooks/useEnvironmentManagement';
import type { LogFilter } from './components/services/types';
import { useAppLogs } from './hooks/useAppLogs';
import { useDarkMode } from './hooks/useDarkMode';
import { useEnvironmentSummary } from './hooks/useEnvironmentSummary';
import { useLogConversion } from './hooks/useLogConversion';

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
  const { globalLogs, deploymentLogs, problems, clearAllLogs } = useAppLogs();
  
  // LogsViewer state
  const [logFilter, setLogFilter] = useState<LogFilter>({
    search: '',
    type: 'all',
    source: 'all',
    severity: 'all',
  });
  const [isAutoScroll, setIsAutoScroll] = useState(true);

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

  const serviceConfigs = getServiceConfigs({}, serviceEnvironments, getEnvironmentUrls);
  const environmentSummary = useEnvironmentSummary(serviceEnvironments);
  const { allLogs, filteredLogs } = useLogConversion(globalLogs, deploymentLogs, problems, logFilter);
  
  const bottomPanelTabs = useAppBottomPanelTabs({
    allLogs,
    filteredLogs,
    problemsCount: problems.length,
    logFilter,
    isAutoScroll,
    onFilterChange: setLogFilter,
    onAutoScrollChange: setIsAutoScroll,
    onClearLogs: clearAllLogs,
  });

  return (
    <VSCodeLayout
      activityBarItems={ACTIVITY_BAR_ITEMS}
      activeActivityId={currentView}
      onActivityChange={(id) => setCurrentView(id as View)}
      primarySidebar={
        <AppSidebar
          currentView={currentView}
          serviceConfigs={serviceConfigs}
          serviceEnvironments={serviceEnvironments}
          activityBarItems={ACTIVITY_BAR_ITEMS}
        />
      }
      primarySidebarTitle={ACTIVITY_BAR_ITEMS.find(a => a.id === currentView)?.title}
      bottomPanelTabs={bottomPanelTabs}
      defaultBottomTab="output"
      statusBarLeft={
        <>
          <span className="flex items-center gap-1">
            <span className="w-2 h-2 rounded-full bg-green-500"></span>
            <span>MYSTIRA DEVHUB</span>
          </span>
          <span className="text-blue-200">{environmentSummary}</span>
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
      <AppContent currentView={currentView} onNavigate={handleNavigate} />
    </VSCodeLayout>
  );
}

export default App;

