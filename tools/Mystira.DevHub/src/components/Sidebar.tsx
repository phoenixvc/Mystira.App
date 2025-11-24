import { useEffect, useRef, useState } from 'react';

export type View = 'services' | 'dashboard' | 'cosmos' | 'migration' | 'infrastructure';

interface SidebarProps {
  currentView: View;
  onViewChange: (view: View) => void;
  onPositionChange?: (position: 'left' | 'right') => void;
  onCollapsedChange?: (collapsed: boolean) => void;
}

export function Sidebar({ currentView, onViewChange, onPositionChange, onCollapsedChange }: SidebarProps) {
  const [isCollapsed, setIsCollapsed] = useState(() => {
    const saved = localStorage.getItem('sidebarCollapsed');
    return saved === 'true';
  });
  const [position, setPosition] = useState<'left' | 'right'>(() => {
    const saved = localStorage.getItem('sidebarPosition');
    return (saved as 'left' | 'right') || 'left';
  });
  const [isDragging, setIsDragging] = useState(false);
  const sidebarRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    localStorage.setItem('sidebarCollapsed', String(isCollapsed));
    onCollapsedChange?.(isCollapsed);
  }, [isCollapsed, onCollapsedChange]);

  useEffect(() => {
    localStorage.setItem('sidebarPosition', position);
    onPositionChange?.(position);
  }, [position, onPositionChange]);

  const handleHeaderMouseDown = (e: React.MouseEvent) => {
    // Only allow dragging from header, not from buttons
    if ((e.target as HTMLElement).closest('button')) return;
    if (isCollapsed) return;
    e.preventDefault();
    setIsDragging(true);
  };

  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (_e: MouseEvent) => {
      if (!sidebarRef.current) return;
      
      const container = sidebarRef.current.parentElement;
      if (!container) return;

      const containerRect = container.getBoundingClientRect();
      const sidebarRect = sidebarRef.current.getBoundingClientRect();
      const centerX = sidebarRect.left + sidebarRect.width / 2;
      const containerCenterX = containerRect.left + containerRect.width / 2;

      // Switch position if dragged past center
      if (centerX < containerCenterX && position === 'right') {
        setPosition('left');
      } else if (centerX >= containerCenterX && position === 'left') {
        setPosition('right');
      }
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, position]);

  const menuItems: { id: View; label: string; icon: string }[] = [
    { id: 'services', label: 'Services', icon: '⚙️' },
    { id: 'dashboard', label: 'Dashboard', icon: '📊' },
    { id: 'cosmos', label: 'Cosmos', icon: '🌌' },
    { id: 'migration', label: 'Migration', icon: '🔄' },
    { id: 'infrastructure', label: 'Infrastructure', icon: '🏗️' },
  ];

  return (
    <div
      ref={sidebarRef}
      className={`fixed top-0 bottom-0 z-40 bg-gray-900 dark:bg-gray-950 border-r border-l border-gray-700 dark:border-gray-800 transition-all duration-300 shadow-lg ${
        position === 'left' ? 'left-0 border-r' : 'right-0 border-l'
      } ${isCollapsed ? 'w-12' : 'w-56'}`}
    >
      {/* Header - Draggable */}
      <div
        className="h-10 flex items-center justify-between px-3 border-b border-gray-700 dark:border-gray-800 bg-gray-800 dark:bg-gray-900 cursor-move select-none"
        onMouseDown={handleHeaderMouseDown}
      >
        {!isCollapsed && (
          <span className="text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest font-mono">
            EXPLORER
          </span>
        )}
        <div className="flex items-center gap-1">
          {!isCollapsed && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                setPosition(position === 'left' ? 'right' : 'left');
              }}
              className="p-1 hover:bg-gray-700 dark:hover:bg-gray-800 rounded text-gray-500 hover:text-gray-300 text-[10px] transition-colors"
              title={`Move to ${position === 'left' ? 'right' : 'left'}`}
            >
              {position === 'left' ? '→' : '←'}
            </button>
          )}
          <button
            onClick={(e) => {
              e.stopPropagation();
              setIsCollapsed(!isCollapsed);
            }}
            className="p-1 hover:bg-gray-700 dark:hover:bg-gray-800 rounded text-gray-500 hover:text-gray-300 text-[10px] transition-colors"
            title={isCollapsed ? 'Expand' : 'Collapse'}
          >
            {isCollapsed ? '▶' : '◀'}
          </button>
        </div>
      </div>

      {/* Menu Items */}
      <nav className="py-1">
        {menuItems.map((item) => (
          <button
            key={item.id}
            onClick={() => onViewChange(item.id)}
            className={`w-full flex items-center gap-2.5 px-3 py-2 text-left transition-colors font-mono ${
              currentView === item.id
                ? 'bg-blue-700 dark:bg-blue-800 text-white border-l-2 border-blue-500'
                : 'text-gray-400 dark:text-gray-500 hover:bg-gray-800 dark:hover:bg-gray-900 hover:text-gray-300 dark:hover:text-gray-400'
            } ${isCollapsed ? 'justify-center px-0' : ''}`}
            title={isCollapsed ? item.label : ''}
          >
            <span className="text-base">{item.icon}</span>
            {!isCollapsed && (
              <span className="text-xs font-medium uppercase tracking-wide">{item.label}</span>
            )}
          </button>
        ))}
      </nav>
    </div>
  );
}

