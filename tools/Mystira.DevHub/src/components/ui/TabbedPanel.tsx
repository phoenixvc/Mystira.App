import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';

// =============================================================================
// Types
// =============================================================================

export interface Tab {
  id: string;
  label: string;
  icon?: ReactNode;
  content: ReactNode;
  badge?: string | number;
  badgeVariant?: 'default' | 'success' | 'error' | 'warning' | 'info';
  disabled?: boolean;
}

export interface TabbedPanelProps {
  tabs: Tab[];
  defaultTab?: string;
  activeTab?: string;
  onTabChange?: (tabId: string) => void;
  storageKey?: string;
  variant?: 'default' | 'pills' | 'underline';
  size?: 'sm' | 'md';
  className?: string;
  tabBarClassName?: string;
  contentClassName?: string;
  rightContent?: ReactNode;
}

// =============================================================================
// Styles
// =============================================================================

const badgeVariants = {
  default: 'bg-gray-500 text-white',
  success: 'bg-green-500 text-white',
  error: 'bg-red-500 text-white',
  warning: 'bg-yellow-500 text-black',
  info: 'bg-blue-500 text-white',
};

const tabSizes = {
  sm: 'px-2 py-1 text-[10px] gap-1',
  md: 'px-3 py-1.5 text-xs gap-1.5',
};

// =============================================================================
// TabbedPanel Component
// =============================================================================

export function TabbedPanel({
  tabs,
  defaultTab,
  activeTab: controlledActiveTab,
  onTabChange,
  storageKey,
  variant = 'default',
  size = 'sm',
  className = '',
  tabBarClassName = '',
  contentClassName = '',
  rightContent,
}: TabbedPanelProps) {
  const isControlled = controlledActiveTab !== undefined;

  const [internalActiveTab, setInternalActiveTab] = useState(() => {
    if (storageKey) {
      const saved = localStorage.getItem(`${storageKey}_activeTab`);
      if (saved && tabs.some(t => t.id === saved)) {
        return saved;
      }
    }
    return defaultTab || tabs[0]?.id || '';
  });

  const activeTab = isControlled ? controlledActiveTab : internalActiveTab;

  useEffect(() => {
    if (storageKey && !isControlled) {
      localStorage.setItem(`${storageKey}_activeTab`, internalActiveTab);
    }
  }, [internalActiveTab, storageKey, isControlled]);

  const handleTabClick = (tabId: string) => {
    if (isControlled) {
      onTabChange?.(tabId);
    } else {
      setInternalActiveTab(tabId);
      onTabChange?.(tabId);
    }
  };

  const getTabStyles = (tab: Tab, isActive: boolean) => {
    const baseStyles = `flex items-center font-medium transition-colors whitespace-nowrap ${tabSizes[size]}`;
    const disabledStyles = tab.disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer';

    switch (variant) {
      case 'pills':
        return `${baseStyles} ${disabledStyles} rounded ${
          isActive
            ? 'bg-blue-600 text-white dark:bg-blue-500'
            : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
        }`;

      case 'underline':
        return `${baseStyles} ${disabledStyles} border-b-2 ${
          isActive
            ? 'border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400'
            : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
        }`;

      default:
        return `${baseStyles} ${disabledStyles} rounded-t ${
          isActive
            ? 'bg-white dark:bg-gray-800 text-blue-600 dark:text-blue-400 border-t border-l border-r border-gray-200 dark:border-gray-600 -mb-px'
            : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700'
        }`;
    }
  };

  return (
    <div className={`flex flex-col h-full ${className}`}>
      {/* Tab Bar */}
      <div
        className={`flex items-center justify-between px-2 py-1 bg-gray-50 dark:bg-gray-800/50 border-b border-gray-200 dark:border-gray-700 ${tabBarClassName}`}
      >
        <div className="flex items-center gap-1 overflow-x-auto">
          {tabs.map(tab => (
            <button
              key={tab.id}
              onClick={() => !tab.disabled && handleTabClick(tab.id)}
              disabled={tab.disabled}
              className={getTabStyles(tab, activeTab === tab.id)}
            >
              {tab.icon && <span>{tab.icon}</span>}
              {tab.label}
              {tab.badge !== undefined && (
                <span
                  className={`ml-1 px-1.5 py-0.5 text-[9px] rounded-full ${
                    badgeVariants[tab.badgeVariant || 'default']
                  }`}
                >
                  {tab.badge}
                </span>
              )}
            </button>
          ))}
        </div>
        {rightContent && <div className="flex items-center gap-1 ml-2">{rightContent}</div>}
      </div>

      {/* Tab Content */}
      <div className={`flex-1 overflow-auto ${contentClassName}`}>
        {tabs.find(t => t.id === activeTab)?.content}
      </div>
    </div>
  );
}

// =============================================================================
// Resizable Panel Component
// =============================================================================

export interface ResizablePanelProps {
  children: ReactNode;
  defaultHeight?: number;
  minHeight?: number;
  maxHeight?: number;
  defaultCollapsed?: boolean;
  storageKey?: string;
  header?: ReactNode;
  onCollapseChange?: (collapsed: boolean) => void;
  className?: string;
}

export function ResizablePanel({
  children,
  defaultHeight = 250,
  minHeight = 100,
  maxHeight = 600,
  defaultCollapsed = false,
  storageKey,
  header,
  onCollapseChange,
  className = '',
}: ResizablePanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  const [isCollapsed, setIsCollapsed] = useState(() => {
    if (storageKey) {
      const saved = localStorage.getItem(`${storageKey}_collapsed`);
      return saved ? JSON.parse(saved) : defaultCollapsed;
    }
    return defaultCollapsed;
  });

  const [height, setHeight] = useState(() => {
    if (storageKey) {
      const saved = localStorage.getItem(`${storageKey}_height`);
      return saved ? parseInt(saved, 10) : defaultHeight;
    }
    return defaultHeight;
  });

  const [isDragging, setIsDragging] = useState(false);

  useEffect(() => {
    if (storageKey) {
      localStorage.setItem(`${storageKey}_collapsed`, JSON.stringify(isCollapsed));
    }
  }, [isCollapsed, storageKey]);

  useEffect(() => {
    if (storageKey) {
      localStorage.setItem(`${storageKey}_height`, String(height));
    }
  }, [height, storageKey]);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);

    const startY = e.clientY;
    const startHeight = height;

    const handleMouseMove = (e: MouseEvent) => {
      const deltaY = startY - e.clientY;
      const newHeight = Math.max(minHeight, Math.min(maxHeight, startHeight + deltaY));
      setHeight(newHeight);
    };

    const handleMouseUp = () => {
      setIsDragging(false);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [height, minHeight, maxHeight]);

  const toggleCollapse = useCallback(() => {
    setIsCollapsed((prev: boolean) => {
      const newValue = !prev;
      onCollapseChange?.(newValue);
      return newValue;
    });
  }, [onCollapseChange]);

  return (
    <div
      ref={containerRef}
      className={`flex flex-col border-t border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 ${className}`}
      style={{ height: isCollapsed ? 'auto' : `${height}px` }}
    >
      {/* Resize Handle */}
      {!isCollapsed && (
        <div
          className={`h-1 cursor-ns-resize bg-gray-200 dark:bg-gray-700 hover:bg-blue-400 dark:hover:bg-blue-500 transition-colors ${
            isDragging ? 'bg-blue-500 dark:bg-blue-400' : ''
          }`}
          onMouseDown={handleMouseDown}
          title="Drag to resize"
        />
      )}

      {/* Header */}
      {header && (
        <div className="flex items-center justify-between px-2 py-1 bg-gray-100 dark:bg-gray-700 border-b border-gray-200 dark:border-gray-600">
          {header}
          <button
            onClick={toggleCollapse}
            className="p-1 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-500 dark:text-gray-400 text-xs"
            title={isCollapsed ? 'Expand panel' : 'Collapse panel'}
          >
            {isCollapsed ? '▲' : '▼'}
          </button>
        </div>
      )}

      {/* Content */}
      {!isCollapsed && <div className="flex-1 overflow-auto">{children}</div>}
    </div>
  );
}

// =============================================================================
// Collapsible Section Component
// =============================================================================

export interface CollapsibleSectionProps {
  title: string;
  icon?: ReactNode;
  defaultExpanded?: boolean;
  children: ReactNode;
  actions?: ReactNode;
  badge?: string | number;
  className?: string;
}

export function CollapsibleSection({
  title,
  icon,
  defaultExpanded = true,
  children,
  actions,
  badge,
  className = '',
}: CollapsibleSectionProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  return (
    <div className={`border-b border-gray-200 dark:border-gray-700 ${className}`}>
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="flex items-center justify-between w-full px-3 py-2 text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider hover:bg-gray-50 dark:hover:bg-gray-800/50"
      >
        <div className="flex items-center gap-2">
          <span className="text-[10px]">{isExpanded ? '▼' : '▶'}</span>
          {icon && <span>{icon}</span>}
          {title}
          {badge !== undefined && (
            <span className="px-1.5 py-0.5 text-[9px] bg-gray-200 dark:bg-gray-600 rounded-full font-normal">
              {badge}
            </span>
          )}
        </div>
        {actions && isExpanded && (
          <div className="flex items-center gap-1" onClick={e => e.stopPropagation()}>
            {actions}
          </div>
        )}
      </button>
      {isExpanded && <div className="px-3 pb-3">{children}</div>}
    </div>
  );
}

export default TabbedPanel;
