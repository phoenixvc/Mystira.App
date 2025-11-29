import type { ActivityBarItem } from '../types';

interface ActivityBarProps {
  items: ActivityBarItem[];
  activeId: string;
  onActivityChange: (id: string) => void;
  onPrimarySidebarToggle: () => void;
  primarySidebarCollapsed: boolean;
  secondarySidebar?: React.ReactNode;
  onSecondarySidebarToggle: () => void;
  secondarySidebarCollapsed: boolean;
  bottomPanelTabs: Array<{ id: string }>;
  onBottomPanelToggle: () => void;
  bottomPanelCollapsed: boolean;
  activityBarWidth: number;
}

export function ActivityBar({
  items,
  activeId,
  onActivityChange,
  onPrimarySidebarToggle,
  primarySidebarCollapsed,
  secondarySidebar,
  onSecondarySidebarToggle,
  secondarySidebarCollapsed,
  bottomPanelTabs,
  onBottomPanelToggle,
  bottomPanelCollapsed,
  activityBarWidth,
}: ActivityBarProps) {
  return (
    <div
      className="flex flex-col bg-gray-800 border-r border-gray-700"
      style={{ width: activityBarWidth }}
    >
      <div className="flex-1 flex flex-col py-1">
        {items.map(item => (
          <button
            key={item.id}
            onClick={() => {
              onActivityChange(item.id);
              if (primarySidebarCollapsed) {
                onPrimarySidebarToggle();
              }
            }}
            className={`relative w-full h-12 flex items-center justify-center transition-colors ${
              activeId === item.id
                ? 'text-white border-l-2 border-blue-500 bg-gray-700/50'
                : 'text-gray-400 hover:text-white border-l-2 border-transparent'
            }`}
            title={item.title}
          >
            <span className="text-xl">{item.icon}</span>
            {item.badge !== undefined && (
              <span className="absolute top-1 right-1 min-w-[16px] h-4 px-1 text-[10px] bg-blue-500 rounded-full flex items-center justify-center">
                {item.badge}
              </span>
            )}
          </button>
        ))}
      </div>

      <div className="border-t border-gray-700 py-1">
        {secondarySidebar && (
          <button
            onClick={onSecondarySidebarToggle}
            className={`w-full h-10 flex items-center justify-center transition-colors ${
              !secondarySidebarCollapsed
                ? 'text-white'
                : 'text-gray-400 hover:text-white'
            }`}
            title={secondarySidebarCollapsed ? 'Show Secondary Sidebar' : 'Hide Secondary Sidebar'}
          >
            <span className="text-lg">⊞</span>
          </button>
        )}
        {bottomPanelTabs.length > 0 && (
          <button
            onClick={onBottomPanelToggle}
            className={`w-full h-10 flex items-center justify-center transition-colors ${
              !bottomPanelCollapsed
                ? 'text-white'
                : 'text-gray-400 hover:text-white'
            }`}
            title={bottomPanelCollapsed ? 'Show Panel' : 'Hide Panel'}
          >
            <span className="text-lg">⊟</span>
          </button>
        )}
      </div>
    </div>
  );
}

