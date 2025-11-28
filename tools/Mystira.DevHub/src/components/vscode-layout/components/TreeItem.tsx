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

