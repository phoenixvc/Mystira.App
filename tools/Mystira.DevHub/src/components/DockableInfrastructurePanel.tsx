import { useCallback, useEffect, useRef, useState } from 'react';

export interface DockablePanelConfig {
  id: string;
  title: string;
  icon?: string;
  content: React.ReactNode;
  defaultPosition: 'left' | 'right' | 'bottom' | 'center';
  defaultSize?: { width?: number; height?: number };
  minSize?: { width?: number; height?: number };
  collapsible?: boolean;
  defaultCollapsed?: boolean;
}

interface DockableLayoutProps {
  panels: DockablePanelConfig[];
  storageKey?: string;
  className?: string;
}

interface PanelState {
  collapsed: boolean;
  size: number; // width for left/right, height for bottom
}

type LayoutState = Record<string, PanelState>;

const DEFAULT_SIZES = {
  left: 280,
  right: 320,
  bottom: 200,
};

const MIN_SIZES = {
  left: 200,
  right: 200,
  bottom: 100,
};

const MAX_SIZES = {
  left: 500,
  right: 600,
  bottom: 500,
};

export function DockableLayout({ panels, storageKey = 'dockableLayout', className = '' }: DockableLayoutProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  // Initialize layout state from localStorage or defaults
  const [layoutState, setLayoutState] = useState<LayoutState>(() => {
    const saved = localStorage.getItem(storageKey);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // Fall through to defaults
      }
    }

    const initial: LayoutState = {};
    panels.forEach(panel => {
      initial[panel.id] = {
        collapsed: panel.defaultCollapsed || false,
        size: panel.defaultPosition === 'bottom'
          ? (panel.defaultSize?.height || DEFAULT_SIZES.bottom)
          : (panel.defaultSize?.width || DEFAULT_SIZES[panel.defaultPosition as keyof typeof DEFAULT_SIZES] || 300),
      };
    });
    return initial;
  });

  // Resizing state
  const [resizing, setResizing] = useState<{ panelId: string; startPos: number; startSize: number } | null>(null);

  // Save layout to localStorage
  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(layoutState));
  }, [layoutState, storageKey]);

  // Handle resize
  const handleResizeStart = useCallback((panelId: string, position: 'left' | 'right' | 'bottom', e: React.MouseEvent) => {
    e.preventDefault();
    const startPos = position === 'bottom' ? e.clientY : e.clientX;
    const startSize = layoutState[panelId]?.size || DEFAULT_SIZES[position];
    setResizing({ panelId, startPos, startSize });
  }, [layoutState]);

  useEffect(() => {
    if (!resizing) return;

    const panel = panels.find(p => p.id === resizing.panelId);
    if (!panel) return;

    const handleMouseMove = (e: MouseEvent) => {
      const position = panel.defaultPosition;
      const currentPos = position === 'bottom' ? e.clientY : e.clientX;
      let delta = resizing.startPos - currentPos;

      // For right panel, delta is inverted
      if (position === 'right') {
        delta = -delta;
      }

      const minSize = panel.minSize
        ? (position === 'bottom' ? panel.minSize.height : panel.minSize.width) || MIN_SIZES[position as keyof typeof MIN_SIZES]
        : MIN_SIZES[position as keyof typeof MIN_SIZES];
      const maxSize = MAX_SIZES[position as keyof typeof MAX_SIZES];

      const newSize = Math.max(minSize, Math.min(maxSize, resizing.startSize + delta));

      setLayoutState(prev => ({
        ...prev,
        [resizing.panelId]: {
          ...prev[resizing.panelId],
          size: newSize,
        },
      }));
    };

    const handleMouseUp = () => {
      setResizing(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [resizing, panels]);

  const toggleCollapse = useCallback((panelId: string) => {
    setLayoutState(prev => ({
      ...prev,
      [panelId]: {
        ...prev[panelId],
        collapsed: !prev[panelId]?.collapsed,
      },
    }));
  }, []);

  // Organize panels by position
  const leftPanels = panels.filter(p => p.defaultPosition === 'left');
  const rightPanels = panels.filter(p => p.defaultPosition === 'right');
  const bottomPanels = panels.filter(p => p.defaultPosition === 'bottom');
  const centerPanels = panels.filter(p => p.defaultPosition === 'center');

  const renderPanel = (panel: DockablePanelConfig, position: 'left' | 'right' | 'bottom') => {
    const state = layoutState[panel.id] || { collapsed: false, size: DEFAULT_SIZES[position] };
    const isHorizontal = position === 'left' || position === 'right';

    return (
      <div
        key={panel.id}
        className={`flex ${isHorizontal ? 'flex-col' : 'flex-row'} bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 ${
          position === 'left' ? 'border-r' : position === 'right' ? 'border-l' : 'border-t'
        }`}
        style={
          state.collapsed
            ? isHorizontal
              ? { width: 'auto' }
              : { height: 'auto' }
            : isHorizontal
              ? { width: `${state.size}px` }
              : { height: `${state.size}px` }
        }
      >
        {/* Panel Header */}
        <div className={`flex items-center justify-between px-3 py-2 bg-gray-100 dark:bg-gray-700 border-b border-gray-200 dark:border-gray-600 ${
          isHorizontal ? '' : 'flex-shrink-0'
        }`}>
          <div className="flex items-center gap-2">
            {panel.icon && <span className="text-sm">{panel.icon}</span>}
            <span className="text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wide">
              {panel.title}
            </span>
          </div>
          {panel.collapsible !== false && (
            <button
              onClick={() => toggleCollapse(panel.id)}
              className="p-1 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-500 dark:text-gray-400 text-xs"
              title={state.collapsed ? 'Expand' : 'Collapse'}
            >
              {state.collapsed
                ? (position === 'left' ? '▶' : position === 'right' ? '◀' : '▲')
                : (position === 'left' ? '◀' : position === 'right' ? '▶' : '▼')
              }
            </button>
          )}
        </div>

        {/* Panel Content */}
        {!state.collapsed && (
          <div className="flex-1 overflow-auto">
            {panel.content}
          </div>
        )}

        {/* Resize Handle */}
        {!state.collapsed && (
          <div
            className={`${
              isHorizontal
                ? `absolute top-0 ${position === 'left' ? 'right-0' : 'left-0'} w-1 h-full cursor-ew-resize hover:bg-blue-400 dark:hover:bg-blue-500`
                : 'absolute left-0 top-0 h-1 w-full cursor-ns-resize hover:bg-blue-400 dark:hover:bg-blue-500'
            } bg-transparent transition-colors ${resizing?.panelId === panel.id ? 'bg-blue-500' : ''}`}
            onMouseDown={(e) => handleResizeStart(panel.id, position, e)}
          />
        )}
      </div>
    );
  };

  return (
    <div ref={containerRef} className={`flex flex-col h-full ${className}`}>
      <div className="flex flex-1 min-h-0">
        {/* Left Panels */}
        {leftPanels.length > 0 && (
          <div className="flex relative">
            {leftPanels.map(p => renderPanel(p, 'left'))}
          </div>
        )}

        {/* Center Content */}
        <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
          {centerPanels.map(panel => (
            <div key={panel.id} className="flex-1 overflow-auto">
              {panel.content}
            </div>
          ))}
        </div>

        {/* Right Panels */}
        {rightPanels.length > 0 && (
          <div className="flex relative">
            {rightPanels.map(p => renderPanel(p, 'right'))}
          </div>
        )}
      </div>

      {/* Bottom Panels */}
      {bottomPanels.length > 0 && (
        <div className="flex flex-col relative">
          {bottomPanels.map(p => renderPanel(p, 'bottom'))}
        </div>
      )}
    </div>
  );
}

// Simple tabbed panel component for grouping multiple content areas
interface TabbedPanelProps {
  tabs: Array<{
    id: string;
    label: string;
    icon?: string;
    content: React.ReactNode;
    badge?: string | number;
  }>;
  defaultTab?: string;
  storageKey?: string;
}

export function TabbedPanel({ tabs, defaultTab, storageKey }: TabbedPanelProps) {
  const [activeTab, setActiveTab] = useState(() => {
    if (storageKey) {
      const saved = localStorage.getItem(`${storageKey}_activeTab`);
      if (saved && tabs.some(t => t.id === saved)) {
        return saved;
      }
    }
    return defaultTab || tabs[0]?.id || '';
  });

  useEffect(() => {
    if (storageKey) {
      localStorage.setItem(`${storageKey}_activeTab`, activeTab);
    }
  }, [activeTab, storageKey]);

  return (
    <div className="flex flex-col h-full">
      {/* Tab Bar */}
      <div className="flex items-center gap-1 px-2 py-1 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700 overflow-x-auto">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-t transition-colors whitespace-nowrap ${
              activeTab === tab.id
                ? 'bg-white dark:bg-gray-800 text-blue-600 dark:text-blue-400 border-t border-l border-r border-gray-200 dark:border-gray-600 -mb-px'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700'
            }`}
          >
            {tab.icon && <span>{tab.icon}</span>}
            {tab.label}
            {tab.badge !== undefined && (
              <span className="ml-1 px-1.5 py-0.5 text-[10px] bg-gray-200 dark:bg-gray-600 rounded-full">
                {tab.badge}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-auto">
        {tabs.find(t => t.id === activeTab)?.content}
      </div>
    </div>
  );
}
