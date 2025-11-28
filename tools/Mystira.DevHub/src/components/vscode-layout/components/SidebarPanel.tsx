import { useState } from 'react';

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

