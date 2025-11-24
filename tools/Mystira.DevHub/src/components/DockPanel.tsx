import { useCallback, useEffect, useRef, useState } from 'react';

export interface DockPanelConfig {
  id: string;
  title: string;
  component: React.ReactNode;
  defaultPosition?: { x: number; y: number };
  defaultSize?: { width: number; height: number };
  minSize?: { width: number; height: number };
  maxSize?: { width: number; height: number };
  defaultCollapsed?: boolean;
  resizable?: boolean;
  collapsible?: boolean;
  order?: number;
}

interface DockPanelProps {
  panels: DockPanelConfig[];
  onLayoutChange?: (layout: DockLayout) => void;
}

export interface DockLayout {
  [panelId: string]: {
    x: number;
    y: number;
    width: number;
    height: number;
    collapsed: boolean;
    zIndex: number;
  };
}

interface PanelState {
  x: number;
  y: number;
  width: number;
  height: number;
  collapsed: boolean;
  zIndex: number;
  isDragging: boolean;
  isResizing: boolean;
  dragStart?: { x: number; y: number };
  resizeStart?: { x: number; y: number; width: number; height: number };
}

export function DockPanel({ panels, onLayoutChange }: DockPanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [panelStates, setPanelStates] = useState<Record<string, PanelState>>(() => {
    const saved = localStorage.getItem('dockLayout');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        return panels.reduce((acc, panel, index) => {
          const savedState = parsed[panel.id];
          acc[panel.id] = {
            x: savedState?.x ?? panel.defaultPosition?.x ?? 50 + index * 30,
            y: savedState?.y ?? panel.defaultPosition?.y ?? 50 + index * 30,
            width: savedState?.width ?? panel.defaultSize?.width ?? 400,
            height: savedState?.height ?? panel.defaultSize?.height ?? 300,
            collapsed: savedState?.collapsed ?? panel.defaultCollapsed ?? false,
            zIndex: savedState?.zIndex ?? index + 1,
            isDragging: false,
            isResizing: false,
          };
          return acc;
        }, {} as Record<string, PanelState>);
      } catch {
        // Fall through to default
      }
    }
    
    return panels.reduce((acc, panel, index) => {
      acc[panel.id] = {
        x: panel.defaultPosition?.x ?? 50 + index * 30,
        y: panel.defaultPosition?.y ?? 50 + index * 30,
        width: panel.defaultSize?.width ?? 400,
        height: panel.defaultSize?.height ?? 300,
        collapsed: panel.defaultCollapsed ?? false,
        zIndex: index + 1,
        isDragging: false,
        isResizing: false,
      };
      return acc;
    }, {} as Record<string, PanelState>);
  });

  const [maxZIndex, setMaxZIndex] = useState(panels.length);

  const saveLayout = useCallback(() => {
    const layout: DockLayout = {};
    Object.entries(panelStates).forEach(([id, state]) => {
      layout[id] = {
        x: state.x,
        y: state.y,
        width: state.width,
        height: state.height,
        collapsed: state.collapsed,
        zIndex: state.zIndex,
      };
    });
    localStorage.setItem('dockLayout', JSON.stringify(layout));
    onLayoutChange?.(layout);
  }, [panelStates, onLayoutChange]);

  useEffect(() => {
    saveLayout();
  }, [saveLayout]);

  const bringToFront = useCallback((panelId: string) => {
    setPanelStates(prev => {
      const newMaxZ = maxZIndex + 1;
      setMaxZIndex(newMaxZ);
      return {
        ...prev,
        [panelId]: {
          ...prev[panelId],
          zIndex: newMaxZ,
        },
      };
    });
  }, [maxZIndex]);

  const handleMouseDown = useCallback((panelId: string, e: React.MouseEvent, type: 'drag' | 'resize') => {
    e.preventDefault();
    e.stopPropagation();
    bringToFront(panelId);

    const panelState = panelStates[panelId];
    if (!panelState) return;

    const startX = e.clientX;
    const startY = e.clientY;

    if (type === 'drag') {
      setPanelStates(prev => ({
        ...prev,
        [panelId]: {
          ...prev[panelId],
          isDragging: true,
          dragStart: { x: startX - prev[panelId].x, y: startY - prev[panelId].y },
        },
      }));
    } else {
      setPanelStates(prev => ({
        ...prev,
        [panelId]: {
          ...prev[panelId],
          isResizing: true,
          resizeStart: { x: startX, y: startY, width: prev[panelId].width, height: prev[panelId].height },
        },
      }));
    }

    const handleMouseMove = (e: MouseEvent) => {
      setPanelStates(prev => {
        const state = prev[panelId];
        if (!state) return prev;

        if (state.isDragging && state.dragStart) {
          const newX = e.clientX - state.dragStart.x;
          const newY = e.clientY - state.dragStart.y;
          
          const container = containerRef.current;
          if (container) {
            const maxX = container.clientWidth - state.width;
            const maxY = container.clientHeight - state.height;
            
            return {
              ...prev,
              [panelId]: {
                ...state,
                x: Math.max(0, Math.min(newX, maxX)),
                y: Math.max(0, Math.min(newY, maxY)),
              },
            };
          }
        } else if (state.isResizing && state.resizeStart) {
          const panel = panels.find(p => p.id === panelId);
          const minWidth = panel?.minSize?.width ?? 200;
          const minHeight = panel?.minSize?.height ?? 150;
          const maxWidth = panel?.maxSize?.width ?? Infinity;
          const maxHeight = panel?.maxSize?.height ?? Infinity;

          const deltaX = e.clientX - state.resizeStart.x;
          const deltaY = e.clientY - state.resizeStart.y;
          
          const newWidth = Math.max(minWidth, Math.min(state.resizeStart.width + deltaX, maxWidth));
          const newHeight = Math.max(minHeight, Math.min(state.resizeStart.height + deltaY, maxHeight));
          
          return {
            ...prev,
            [panelId]: {
              ...state,
              width: newWidth,
              height: newHeight,
            },
          };
        }
        return prev;
      });
    };

    const handleMouseUp = () => {
      setPanelStates(prev => ({
        ...prev,
        [panelId]: {
          ...prev[panelId],
          isDragging: false,
          isResizing: false,
        },
      }));
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [panelStates, panels, bringToFront]);

  const toggleCollapse = useCallback((panelId: string) => {
    setPanelStates(prev => ({
      ...prev,
      [panelId]: {
        ...prev[panelId],
        collapsed: !prev[panelId].collapsed,
      },
    }));
  }, []);

  return (
    <div ref={containerRef} className="relative w-full h-screen overflow-hidden bg-gray-50 dark:bg-gray-900">
      {panels.map(panel => {
        const state = panelStates[panel.id];
        if (!state) return null;

        const isCollapsible = panel.collapsible !== false;
        const isResizable = panel.resizable !== false && !state.collapsed;

        return (
          <div
            key={panel.id}
            className="absolute bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md shadow-lg flex flex-col"
            style={{
              left: `${state.x}px`,
              top: `${state.y}px`,
              width: state.collapsed ? 'auto' : `${state.width}px`,
              height: state.collapsed ? 'auto' : `${state.height}px`,
              zIndex: state.zIndex,
              minWidth: state.collapsed ? 'auto' : (panel.minSize?.width ?? 200),
              minHeight: state.collapsed ? 'auto' : (panel.minSize?.height ?? 150),
            }}
          >
            {/* Header */}
            <div
              className="flex items-center justify-between px-3 py-2 bg-gray-100 dark:bg-gray-700 border-b border-gray-300 dark:border-gray-600 cursor-move select-none"
              onMouseDown={(e) => handleMouseDown(panel.id, e, 'drag')}
            >
              <div className="flex items-center gap-2 flex-1">
                <span className="text-xs font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wide">
                  {panel.title}
                </span>
              </div>
              <div className="flex items-center gap-1">
                {isCollapsible && (
                  <button
                    onClick={() => toggleCollapse(panel.id)}
                    className="p-1 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-600 dark:text-gray-400 text-xs"
                    title={state.collapsed ? 'Expand' : 'Collapse'}
                  >
                    {state.collapsed ? '▶' : '▼'}
                  </button>
                )}
              </div>
            </div>

            {/* Content */}
            {!state.collapsed && (
              <div className="flex-1 overflow-auto">
                {panel.component}
              </div>
            )}

            {/* Resize Handle */}
            {isResizable && (
              <div
                className="absolute bottom-0 right-0 w-4 h-4 cursor-nwse-resize"
                onMouseDown={(e) => handleMouseDown(panel.id, e, 'resize')}
                title="Resize"
              >
                <div className="absolute bottom-0 right-0 w-0 h-0 border-l-[12px] border-l-transparent border-b-[12px] border-b-gray-400 dark:border-b-gray-500 opacity-50 hover:opacity-100 transition-opacity" />
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

