interface SidebarProps {
  children: React.ReactNode;
  title?: string;
  collapsed: boolean;
  width: number;
  onToggle: () => void;
  onStartResize: (e: React.MouseEvent) => void;
  resizing: boolean;
  position: 'left' | 'right';
}

export function Sidebar({
  children,
  title,
  collapsed,
  width,
  onToggle,
  onStartResize,
  resizing,
  position,
}: SidebarProps) {
  return (
    <div
      className={`flex flex-col bg-gray-800 border-${position === 'left' ? 'r' : 'l'} border-gray-700 relative transition-all duration-200 ${
        collapsed ? 'w-0 overflow-hidden' : ''
      }`}
      style={{ width: collapsed ? 0 : width }}
    >
      {!collapsed && (
        <>
          {position === 'left' && (
            <div
              className={`absolute top-0 right-0 w-1 h-full cursor-ew-resize hover:bg-blue-500 transition-colors ${
                resizing ? 'bg-blue-500' : 'bg-transparent'
              }`}
              onMouseDown={onStartResize}
            />
          )}
          {position === 'right' && (
            <div
              className={`absolute top-0 left-0 w-1 h-full cursor-ew-resize hover:bg-blue-500 transition-colors ${
                resizing ? 'bg-blue-500' : 'bg-transparent'
              }`}
              onMouseDown={onStartResize}
            />
          )}

          {title && (
            <div className="flex items-center justify-between px-4 py-2 border-b border-gray-700">
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
                {title}
              </span>
              <button
                onClick={onToggle}
                className="p-1 text-gray-400 hover:text-white rounded hover:bg-gray-700"
                title="Hide Sidebar"
              >
                <span className="text-xs">✕</span>
              </button>
            </div>
          )}

          <div className="flex-1 overflow-auto">
            {children}
          </div>
        </>
      )}
    </div>
  );
}

