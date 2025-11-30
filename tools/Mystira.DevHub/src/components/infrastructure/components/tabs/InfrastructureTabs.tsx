type Tab = 'actions' | 'smart-deploy' | 'templates' | 'resources' | 'history' | 'recommended-fixes';

interface InfrastructureTabsProps {
  activeTab: Tab;
  onTabChange: (tab: Tab) => void;
}

const tabDescriptions: Record<Tab, string> = {
  'actions': 'Plan and deploy infrastructure templates',
  'smart-deploy': 'Auto-fallback deployment with region prioritization',
  'templates': 'View and manage infrastructure template files',
  'resources': 'View deployed Azure resources',
  'history': 'View deployment history and logs',
  'recommended-fixes': 'View recommended security fixes and improvements',
};

export function InfrastructureTabs({ activeTab, onTabChange }: InfrastructureTabsProps) {
  const tabs: { id: Tab; icon: string; label: string }[] = [
    { id: 'actions', icon: '⚡', label: 'Actions' },
    { id: 'smart-deploy', icon: '🚀', label: 'Smart Deploy' },
    { id: 'templates', icon: '📄', label: 'Templates & Resources' },
    { id: 'resources', icon: '☁️', label: 'Azure Resources' },
    { id: 'history', icon: '📜', label: 'History' },
    { id: 'recommended-fixes', icon: '🔧', label: 'Recommended Fixes' },
  ];

  const handleKeyDown = (e: React.KeyboardEvent, currentIndex: number) => {
    if (e.key === 'ArrowRight') {
      e.preventDefault();
      const nextIndex = (currentIndex + 1) % tabs.length;
      onTabChange(tabs[nextIndex].id);
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      const prevIndex = currentIndex === 0 ? tabs.length - 1 : currentIndex - 1;
      onTabChange(tabs[prevIndex].id);
    }
  };

  return (
    <div className="mb-6">
      <nav
        className="flex space-x-1 border-b border-gray-200 dark:border-gray-700"
        role="tablist"
        aria-label="Infrastructure navigation"
      >
        {tabs.map((tab, index) => (
          <button
            key={tab.id}
            onClick={() => onTabChange(tab.id)}
            onKeyDown={(e) => handleKeyDown(e, index)}
            role="tab"
            aria-selected={activeTab === tab.id}
            aria-controls={`${tab.id}-panel`}
            tabIndex={activeTab === tab.id ? 0 : -1}
            title={tabDescriptions[tab.id]}
            className={`px-4 py-3 text-sm font-medium transition-all duration-200 border-b-2
              focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-900
              ${activeTab === tab.id
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400 bg-blue-50/50 dark:bg-blue-900/20'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800/50'
              }`}
          >
            <span className="flex items-center gap-1.5">
              <span>{tab.icon}</span>
              <span>{tab.label}</span>
            </span>
          </button>
        ))}
      </nav>
    </div>
  );
}

