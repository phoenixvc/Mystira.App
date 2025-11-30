type Tab = 'actions' | 'templates' | 'resources' | 'history' | 'recommended-fixes';

interface InfrastructureTabsProps {
  activeTab: Tab;
  onTabChange: (tab: Tab) => void;
}

export function InfrastructureTabs({ activeTab, onTabChange }: InfrastructureTabsProps) {
  const tabs: { id: Tab; icon: string; label: string }[] = [
    { id: 'actions', icon: '⚡', label: 'Actions' },
    { id: 'templates', icon: '📄', label: 'Templates & Resources' },
    { id: 'resources', icon: '☁️', label: 'Azure Resources' },
    { id: 'history', icon: '📜', label: 'History' },
    { id: 'recommended-fixes', icon: '🔧', label: 'Recommended Fixes' },
  ];

  return (
    <div className="mb-6">
      <nav className="flex space-x-1 border-b border-gray-200 dark:border-gray-700">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => onTabChange(tab.id)}
            className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
              activeTab === tab.id
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
            }`}
          >
            {tab.icon} {tab.label}
          </button>
        ))}
      </nav>
    </div>
  );
}

