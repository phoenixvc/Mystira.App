import { useCallback, useEffect, useRef, useState } from 'react';

// =============================================================================
// Types
// =============================================================================

export interface ActivityBarItem {
  id: string;
  icon: string;
  title: string;
  badge?: number | string;
}

export interface PanelConfig {
  id: string;
  title: string;
  icon?: string;
  content: React.ReactNode;
  closable?: boolean;
  badge?: number | string;
}

export interface BottomPanelTab {
  id: string;
  title: string;
  icon?: string;
  content: React.ReactNode;
  badge?: number | string;
}

export interface VSCodeLayoutProps {
  // Activity bar (leftmost icons)
  activityBarItems: ActivityBarItem[];
  activeActivityId: string;
  onActivityChange: (id: string) => void;

  // Primary sidebar (left panel content)
  primarySidebar?: React.ReactNode;
  primarySidebarTitle?: string;

  // Main content area
  children: React.ReactNode;

  // Secondary sidebar (right panel)
  secondarySidebar?: React.ReactNode;
  secondarySidebarTitle?: string;

  // Bottom panel (logs, output, terminal, etc.)
  bottomPanelTabs?: BottomPanelTab[];
  defaultBottomTab?: string;

  // Status bar
  statusBarLeft?: React.ReactNode;
  statusBarRight?: React.ReactNode;

  // Callbacks
  onLayoutChange?: (layout: LayoutState) => void;

  // Storage key for persistence
  storageKey?: string;
}

export interface LayoutState {
  primarySidebarWidth: number;
  primarySidebarCollapsed: boolean;
  secondarySidebarWidth: number;
  secondarySidebarCollapsed: boolean;
  bottomPanelHeight: number;
  bottomPanelCollapsed: boolean;
  activeBottomTab: string;
}

// =============================================================================
// Constants
// =============================================================================

const DEFAULT_LAYOUT: LayoutState = {
  primarySidebarWidth: 280,
  primarySidebarCollapsed: false,
  secondarySidebarWidth: 320,
  secondarySidebarCollapsed: true,
  bottomPanelHeight: 250,
  bottomPanelCollapsed: true,
  activeBottomTab: '',
};

const MIN_SIZES = {
  primarySidebar: 200,
  secondarySidebar: 200,
  bottomPanel: 100,
};

const MAX_SIZES = {
  primarySidebar: 500,
  secondarySidebar: 600,
  bottomPanel: 500,
};

const ACTIVITY_BAR_WIDTH = 48;

// =============================================================================
// Main Component
// =============================================================================

export function VSCodeLayout({
  activityBarItems,
  activeActivityId,
  onActivityChange,
  primarySidebar,
  primarySidebarTitle,
  children,
  secondarySidebar,
  secondarySidebarTitle,
  bottomPanelTabs = [],
  defaultBottomTab,
  statusBarLeft,
  statusBarRight,
  onLayoutChange,
  storageKey = 'vscodeLayout',
}: VSCodeLayoutProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  // Load/initialize layout state
  const [layout, setLayout] = useState<LayoutState>(() => {
    const saved = localStorage.getItem(storageKey);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        return { ...DEFAULT_LAYOUT, ...parsed };
      } catch {
        // Fall through to default
      }
    }
    return { ...DEFAULT_LAYOUT, activeBottomTab: defaultBottomTab || bottomPanelTabs[0]?.id || '' };
  });

  // Resizing state
  const [resizing, setResizing] = useState<{
    type: 'primary' | 'secondary' | 'bottom';
    startPos: number;
    startSize: number;
  } | null>(null);

  // Persist layout changes
  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(layout));
    onLayoutChange?.(layout);
  }, [layout, storageKey, onLayoutChange]);

  // Handle resize mouse events
  useEffect(() => {
    if (!resizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      const { type, startPos, startSize } = resizing;

      if (type === 'primary') {
        const delta = e.clientX - startPos;
        const newWidth = Math.max(MIN_SIZES.primarySidebar, Math.min(MAX_SIZES.primarySidebar, startSize + delta));
        setLayout(prev => ({ ...prev, primarySidebarWidth: newWidth }));
      } else if (type === 'secondary') {
        const delta = startPos - e.clientX;
        const newWidth = Math.max(MIN_SIZES.secondarySidebar, Math.min(MAX_SIZES.secondarySidebar, startSize + delta));
        setLayout(prev => ({ ...prev, secondarySidebarWidth: newWidth }));
      } else if (type === 'bottom') {
        const delta = startPos - e.clientY;
        const newHeight = Math.max(MIN_SIZES.bottomPanel, Math.min(MAX_SIZES.bottomPanel, startSize + delta));
        setLayout(prev => ({ ...prev, bottomPanelHeight: newHeight }));
      }
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
  }, [resizing]);

  const startResize = useCallback((type: 'primary' | 'secondary' | 'bottom', e: React.MouseEvent) => {
    e.preventDefault();
    const startPos = type === 'bottom' ? e.clientY : e.clientX;
    const startSize = type === 'primary'
      ? layout.primarySidebarWidth
      : type === 'secondary'
        ? layout.secondarySidebarWidth
        : layout.bottomPanelHeight;
    setResizing({ type, startPos, startSize });
  }, [layout]);

  const togglePrimarySidebar = useCallback(() => {
    setLayout(prev => ({ ...prev, primarySidebarCollapsed: !prev.primarySidebarCollapsed }));
  }, []);

  const toggleSecondarySidebar = useCallback(() => {
    setLayout(prev => ({ ...prev, secondarySidebarCollapsed: !prev.secondarySidebarCollapsed }));
  }, []);

  const toggleBottomPanel = useCallback(() => {
    setLayout(prev => ({ ...prev, bottomPanelCollapsed: !prev.bottomPanelCollapsed }));
  }, []);

  const setActiveBottomTab = useCallback((tabId: string) => {
    setLayout(prev => ({
      ...prev,
      activeBottomTab: tabId,
      bottomPanelCollapsed: false,
    }));
  }, []);

  const showPrimarySidebar = primarySidebar && !layout.primarySidebarCollapsed;
  const showSecondarySidebar = secondarySidebar && !layout.secondarySidebarCollapsed;
  const showBottomPanel = bottomPanelTabs.length > 0 && !layout.bottomPanelCollapsed;

  return (
    <div ref={containerRef} className="flex flex-col h-screen bg-gray-900 text-white overflow-hidden">
      {/* Main Content Area (everything except status bar) */}
      <div className="flex flex-1 min-h-0">
        {/* Activity Bar (leftmost) */}
        <div
          className="flex flex-col bg-gray-800 border-r border-gray-700"
          style={{ width: ACTIVITY_BAR_WIDTH }}
        >
          <div className="flex-1 flex flex-col py-1">
            {activityBarItems.map(item => (
              <button
                key={item.id}
                onClick={() => {
                  onActivityChange(item.id);
                  if (layout.primarySidebarCollapsed) {
                    togglePrimarySidebar();
                  }
                }}
                className={`relative w-full h-12 flex items-center justify-center transition-colors ${
                  activeActivityId === item.id
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

          {/* Bottom Activity Bar Icons */}
          <div className="border-t border-gray-700 py-1">
            {secondarySidebar && (
              <button
                onClick={toggleSecondarySidebar}
                className={`w-full h-10 flex items-center justify-center transition-colors ${
                  !layout.secondarySidebarCollapsed
                    ? 'text-white'
                    : 'text-gray-400 hover:text-white'
                }`}
                title={layout.secondarySidebarCollapsed ? 'Show Secondary Sidebar' : 'Hide Secondary Sidebar'}
              >
                <span className="text-lg">⊞</span>
              </button>
            )}
            {bottomPanelTabs.length > 0 && (
              <button
                onClick={toggleBottomPanel}
                className={`w-full h-10 flex items-center justify-center transition-colors ${
                  !layout.bottomPanelCollapsed
                    ? 'text-white'
                    : 'text-gray-400 hover:text-white'
                }`}
                title={layout.bottomPanelCollapsed ? 'Show Panel' : 'Hide Panel'}
              >
                <span className="text-lg">⊟</span>
              </button>
            )}
          </div>
        </div>

        {/* Primary Sidebar */}
        {primarySidebar && (
          <div
            className={`flex flex-col bg-gray-800 border-r border-gray-700 relative transition-all duration-200 ${
              layout.primarySidebarCollapsed ? 'w-0 overflow-hidden' : ''
            }`}
            style={{ width: layout.primarySidebarCollapsed ? 0 : layout.primarySidebarWidth }}
          >
            {showPrimarySidebar && (
              <>
                {/* Sidebar Header */}
                {primarySidebarTitle && (
                  <div className="flex items-center justify-between px-4 py-2 border-b border-gray-700">
                    <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      {primarySidebarTitle}
                    </span>
                    <button
                      onClick={togglePrimarySidebar}
                      className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                      title="Hide Sidebar"
                    >
                      <span className="text-xs">✕</span>
                    </button>
                  </div>
                )}

                {/* Sidebar Content */}
                <div className="flex-1 overflow-auto">
                  {primarySidebar}
                </div>

                {/* Resize Handle */}
                <div
                  className={`absolute top-0 right-0 w-1 h-full cursor-ew-resize hover:bg-blue-500 transition-colors ${
                    resizing?.type === 'primary' ? 'bg-blue-500' : 'bg-transparent'
                  }`}
                  onMouseDown={(e) => startResize('primary', e)}
                />
              </>
            )}
          </div>
        )}

        {/* Center Area (Editor + Bottom Panel) */}
        <div className="flex-1 flex flex-col min-w-0 min-h-0">
          {/* Main Editor Area */}
          <div className="flex-1 overflow-auto bg-gray-900">
            {children}
          </div>

          {/* Bottom Panel */}
          {bottomPanelTabs.length > 0 && (
            <div
              className={`flex flex-col bg-gray-800 border-t border-gray-700 relative transition-all duration-200 ${
                layout.bottomPanelCollapsed ? 'h-0 overflow-hidden' : ''
              }`}
              style={{ height: layout.bottomPanelCollapsed ? 0 : layout.bottomPanelHeight }}
            >
              {showBottomPanel && (
                <>
                  {/* Resize Handle */}
                  <div
                    className={`absolute top-0 left-0 right-0 h-1 cursor-ns-resize hover:bg-blue-500 transition-colors ${
                      resizing?.type === 'bottom' ? 'bg-blue-500' : 'bg-transparent'
                    }`}
                    onMouseDown={(e) => startResize('bottom', e)}
                  />

                  {/* Tab Bar */}
                  <div className="flex items-center justify-between px-2 border-b border-gray-700 flex-shrink-0">
                    <div className="flex items-center gap-1 overflow-x-auto py-1">
                      {bottomPanelTabs.map(tab => (
                        <button
                          key={tab.id}
                          onClick={() => setActiveBottomTab(tab.id)}
                          className={`flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded transition-colors whitespace-nowrap ${
                            layout.activeBottomTab === tab.id
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
                        onClick={toggleBottomPanel}
                        className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                        title="Hide Panel"
                      >
                        <span className="text-xs">▼</span>
                      </button>
                      <button
                        onClick={() => setLayout(prev => ({ ...prev, bottomPanelCollapsed: true }))}
                        className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                        title="Close Panel"
                      >
                        <span className="text-xs">✕</span>
                      </button>
                    </div>
                  </div>

                  {/* Tab Content */}
                  <div className="flex-1 overflow-auto">
                    {bottomPanelTabs.find(t => t.id === layout.activeBottomTab)?.content}
                  </div>
                </>
              )}
            </div>
          )}
        </div>

        {/* Secondary Sidebar (Right) */}
        {secondarySidebar && (
          <div
            className={`flex flex-col bg-gray-800 border-l border-gray-700 relative transition-all duration-200 ${
              layout.secondarySidebarCollapsed ? 'w-0 overflow-hidden' : ''
            }`}
            style={{ width: layout.secondarySidebarCollapsed ? 0 : layout.secondarySidebarWidth }}
          >
            {showSecondarySidebar && (
              <>
                {/* Resize Handle */}
                <div
                  className={`absolute top-0 left-0 w-1 h-full cursor-ew-resize hover:bg-blue-500 transition-colors ${
                    resizing?.type === 'secondary' ? 'bg-blue-500' : 'bg-transparent'
                  }`}
                  onMouseDown={(e) => startResize('secondary', e)}
                />

                {/* Sidebar Header */}
                {secondarySidebarTitle && (
                  <div className="flex items-center justify-between px-4 py-2 border-b border-gray-700">
                    <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                      {secondarySidebarTitle}
                    </span>
                    <button
                      onClick={toggleSecondarySidebar}
                      className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                      title="Hide Sidebar"
                    >
                      <span className="text-xs">✕</span>
                    </button>
                  </div>
                )}

                {/* Sidebar Content */}
                <div className="flex-1 overflow-auto">
                  {secondarySidebar}
                </div>
              </>
            )}
          </div>
        )}
      </div>

      {/* Status Bar */}
      <div className="flex items-center justify-between px-3 py-1 bg-blue-600 text-white text-xs flex-shrink-0">
        <div className="flex items-center gap-3">
          {statusBarLeft}
        </div>
        <div className="flex items-center gap-3">
          {statusBarRight}
        </div>
      </div>
    </div>
  );
}

// =============================================================================
// Helper Components
// =============================================================================

/**
 * A simple panel wrapper for use within VSCodeLayout sidebars
 */
interface SidebarPanelProps {
  title: string;
  icon?: string;
  children: React.ReactNode;
  defaultCollapsed?: boolean;
  actions?: React.ReactNode;
}

export function SidebarPanel({ title, icon, children, defaultCollapsed = false, actions }: SidebarPanelProps) {
  const [collapsed, setCollapsed] = useState(defaultCollapsed);

  return (
    <div className="border-b border-gray-700">
      <button
        onClick={() => setCollapsed(!collapsed)}
        className="flex items-center justify-between w-full px-3 py-2 text-xs font-semibold text-gray-300 uppercase tracking-wider hover:bg-gray-700/50"
      >
        <div className="flex items-center gap-2">
          <span className="text-[10px]">{collapsed ? '▶' : '▼'}</span>
          {icon && <span>{icon}</span>}
          {title}
        </div>
        {actions && !collapsed && (
          <div className="flex items-center gap-1" onClick={e => e.stopPropagation()}>
            {actions}
          </div>
        )}
      </button>
      {!collapsed && (
        <div className="px-2 pb-2">
          {children}
        </div>
      )}
    </div>
  );
}

/**
 * Tree view item for file/resource explorers
 */
interface TreeItemProps {
  label: string;
  icon?: string;
  depth?: number;
  isExpanded?: boolean;
  isSelected?: boolean;
  hasChildren?: boolean;
  onClick?: () => void;
  onToggle?: () => void;
  children?: React.ReactNode;
}

export function TreeItem({
  label,
  icon,
  depth = 0,
  isExpanded = false,
  isSelected = false,
  hasChildren = false,
  onClick,
  onToggle,
  children,
}: TreeItemProps) {
  return (
    <div>
      <button
        onClick={() => {
          if (hasChildren && onToggle) onToggle();
          if (onClick) onClick();
        }}
        className={`flex items-center gap-1 w-full px-2 py-0.5 text-xs hover:bg-gray-700/50 ${
          isSelected ? 'bg-gray-700 text-white' : 'text-gray-300'
        }`}
        style={{ paddingLeft: `${8 + depth * 16}px` }}
      >
        {hasChildren && (
          <span className="text-[10px] w-3">{isExpanded ? '▼' : '▶'}</span>
        )}
        {!hasChildren && <span className="w-3" />}
        {icon && <span className="text-sm">{icon}</span>}
        <span className="truncate">{label}</span>
      </button>
      {isExpanded && children && (
        <div>{children}</div>
      )}
    </div>
  );
}

/**
 * Output panel content wrapper with scroll-to-bottom behavior
 */
interface OutputPanelProps {
  children: React.ReactNode;
  autoScroll?: boolean;
  className?: string;
}

export function OutputPanel({ children, autoScroll = true, className = '' }: OutputPanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isAtBottom, setIsAtBottom] = useState(true);

  useEffect(() => {
    if (autoScroll && isAtBottom && containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [children, autoScroll, isAtBottom]);

  const handleScroll = () => {
    if (!containerRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
    setIsAtBottom(scrollTop + clientHeight >= scrollHeight - 10);
  };

  return (
    <div
      ref={containerRef}
      onScroll={handleScroll}
      className={`h-full overflow-auto font-mono text-xs bg-gray-900 ${className}`}
    >
      {children}
    </div>
  );
}

export default VSCodeLayout;
