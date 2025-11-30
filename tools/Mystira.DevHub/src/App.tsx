import { useEffect, useState } from 'react';
import './App.css';
import { useAppBottomPanelTabs, AppContent, AppSidebar, VSCodeLayout } from './components/app';
import { getServiceConfigs } from './components/services';
import { useEnvironmentManagement } from './components/services/hooks/useEnvironmentManagement';
import type { LogFilter } from './components/services/types';
import { useAppLogs } from './hooks/useAppLogs';
import { useDarkMode } from './hooks/useDarkMode';
import { useEnvironmentSummary } from './hooks/useEnvironmentSummary';
import { useLogConversion } from './hooks/useLogConversion';
import { BOTTOM_PANEL_TABS, EVENTS, LOG_SEVERITY, LOG_SOURCES, LOG_TYPES, STORAGE_KEYS, VIEWS, type View } from './types';

// Activity bar items for main navigation
const ACTIVITY_BAR_ITEMS = [
  { id: VIEWS.SERVICES, icon: '⚡', title: 'Services' },
  { id: VIEWS.DASHBOARD, icon: '📊', title: 'Dashboard' },
  { id: VIEWS.COSMOS, icon: '🔮', title: 'Cosmos Explorer' },
  { id: VIEWS.MIGRATION, icon: '🔄', title: 'Migration Manager' },
  { id: VIEWS.INFRASTRUCTURE, icon: '☁️', title: 'Infrastructure' },
  { id: VIEWS.TEST, icon: '🧪', title: 'Test' },
];

function App() {
  const [currentView, setCurrentView] = useState<View>(VIEWS.SERVICES);
  const { isDark, toggleDarkMode } = useDarkMode();
  const { globalLogs, deploymentLogs, problems, clearAllLogs } = useAppLogs();
  
  // LogsViewer state
  const [logFilter, setLogFilter] = useState<LogFilter>({
    search: '',
    type: LOG_TYPES.ALL,
    source: LOG_SOURCES.ALL,
    severity: LOG_SEVERITY.ALL,
  });
  const [isAutoScroll, setIsAutoScroll] = useState(true);

  // Environment status for header (only when on services view)
  const [serviceEnvironments, setServiceEnvironments] = useState<Record<string, 'local' | 'dev' | 'prod'>>(() => {
    const saved = localStorage.getItem(STORAGE_KEYS.SERVICE_ENVIRONMENTS);
    return saved ? JSON.parse(saved) : {};
  });
  const { getEnvironmentUrls } = useEnvironmentManagement();

  // Listen for environment changes
  useEffect(() => {
    const handleStorageChange = () => {
      const saved = localStorage.getItem(STORAGE_KEYS.SERVICE_ENVIRONMENTS);
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
      setCurrentView(VIEWS.INFRASTRUCTURE);
    };

    window.addEventListener(EVENTS.NAVIGATE_TO_INFRASTRUCTURE, handleNavigateToInfrastructure);
    return () => {
      window.removeEventListener(EVENTS.NAVIGATE_TO_INFRASTRUCTURE, handleNavigateToInfrastructure);
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
      defaultBottomTab={BOTTOM_PANEL_TABS.OUTPUT}
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
      storageKey={STORAGE_KEYS.DEVHUB_LAYOUT}
    >
      <AppContent currentView={currentView} onNavigate={handleNavigate} />
    </VSCodeLayout>
  );
}

export default App;

