import type { BottomPanelTab } from '../types';

interface BottomPanelProps {
  tabs: BottomPanelTab[];
  activeTab: string;
  collapsed: boolean;
  height: number;
  onTabChange: (tabId: string) => void;
  onToggle: () => void;
  onStartResize: (e: React.MouseEvent) => void;
  resizing: boolean;
}

export function BottomPanel({
  tabs,
  activeTab,
  collapsed,
  height,
  onTabChange,
  onToggle,
  onStartResize,
  resizing,
}: BottomPanelProps) {
  if (tabs.length === 0) return null;

  return (
    <div
      className={`flex flex-col bg-gray-800 border-t border-gray-700 relative transition-all duration-200 ${
        collapsed ? 'h-0 overflow-hidden' : ''
      }`}
      style={{ height: collapsed ? 0 : height }}
    >
      {!collapsed && (
        <>
          <div
            className={`absolute top-0 left-0 right-0 h-1 cursor-ns-resize hover:bg-blue-500 transition-colors ${
              resizing ? 'bg-blue-500' : 'bg-transparent'
            }`}
            onMouseDown={onStartResize}
          />

          <div className="flex items-center justify-between px-2 border-b border-gray-700 flex-shrink-0">
            <div className="flex items-center gap-1 overflow-x-auto py-1">
              {tabs.map(tab => (
                <button
                  key={tab.id}
                  onClick={() => onTabChange(tab.id)}
                  className={`flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded transition-colors whitespace-nowrap ${
                    activeTab === tab.id
                      ? 'bg-gray-700 text-white'
                      : 'text-gray-400 hover:text-white hover:bg-gray-700/50'
                  }`}
                >
                  {tab.icon && <span>{tab.icon}</span>}
                  {tab.title}
                  {tab.badge !== undefined && (
                    <span className="ml-1 px-1.5 py-0.5 text-[10px] bg-gray-600 rounded-full">
                      {tab.badge}
                    </span>
                  )}
                </button>
              ))}
            </div>
            <div className="flex items-center gap-1 ml-2">
              <button
                onClick={onToggle}
                className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                title="Hide Panel"
              >
                <span className="text-xs">▼</span>
              </button>
              <button
                onClick={onToggle}
                className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                title="Close Panel"
              >
                <span className="text-xs">✕</span>
              </button>
            </div>
          </div>

          <div className="flex-1 overflow-auto">
            {tabs.find(t => t.id === activeTab)?.content}
          </div>
        </>
      )}
    </div>
  );
}

