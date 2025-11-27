interface ActionButton {
  id: string;
  label: string;
  icon: string;
  onClick: () => void;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'default' | 'primary' | 'success' | 'warning' | 'danger';
  tooltip?: string;
  badge?: string | number;
}

interface ActionToolbarProps {
  actions: ActionButton[];
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

const variantClasses = {
  default: 'border-gray-200 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700',
  primary: 'border-blue-200 dark:border-blue-800 hover:border-blue-400 dark:hover:border-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20',
  success: 'border-green-200 dark:border-green-800 hover:border-green-400 dark:hover:border-green-600 hover:bg-green-50 dark:hover:bg-green-900/20',
  warning: 'border-yellow-200 dark:border-yellow-800 hover:border-yellow-400 dark:hover:border-yellow-600 hover:bg-yellow-50 dark:hover:bg-yellow-900/20',
  danger: 'border-red-200 dark:border-red-800 hover:border-red-400 dark:hover:border-red-600 hover:bg-red-50 dark:hover:bg-red-900/20',
};

const sizeClasses = {
  sm: 'px-2 py-1.5 text-xs gap-1',
  md: 'px-3 py-2 text-sm gap-1.5',
  lg: 'px-4 py-3 text-base gap-2',
};

const iconSizeClasses = {
  sm: 'text-sm',
  md: 'text-lg',
  lg: 'text-xl',
};

export function ActionToolbar({ actions, className = '', size = 'md' }: ActionToolbarProps) {
  return (
    <div className={`flex items-center gap-2 flex-wrap ${className}`}>
      {actions.map((action) => (
        <button
          key={action.id}
          onClick={action.onClick}
          disabled={action.disabled || action.loading}
          className={`
            flex items-center ${sizeClasses[size]}
            bg-white dark:bg-gray-800
            border-2 rounded-lg
            transition-all
            disabled:opacity-50 disabled:cursor-not-allowed
            ${variantClasses[action.variant || 'default']}
          `}
          title={action.tooltip || action.label}
        >
          {action.loading ? (
            <span className={`animate-spin ${iconSizeClasses[size]}`}>⟳</span>
          ) : (
            <span className={iconSizeClasses[size]}>{action.icon}</span>
          )}
          <span className="font-medium text-gray-700 dark:text-gray-200">{action.label}</span>
          {action.badge !== undefined && (
            <span className="ml-1 px-1.5 py-0.5 text-[10px] bg-gray-100 dark:bg-gray-700 rounded-full text-gray-600 dark:text-gray-400">
              {action.badge}
            </span>
          )}
        </button>
      ))}
    </div>
  );
}

// Compact card-style action button (smaller version of original)
interface ActionCardProps {
  icon: string;
  label: string;
  description?: string;
  onClick: () => void;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'default' | 'primary' | 'success' | 'warning' | 'danger';
}

export function ActionCard({ icon, label, description, onClick, disabled, loading, variant = 'default' }: ActionCardProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled || loading}
      className={`
        flex flex-col items-center p-3
        bg-white dark:bg-gray-800
        border-2 rounded-lg
        transition-all
        disabled:opacity-50 disabled:cursor-not-allowed
        min-w-[100px]
        ${variantClasses[variant]}
      `}
    >
      {loading ? (
        <div className="text-2xl mb-1 animate-spin">⟳</div>
      ) : (
        <div className="text-2xl mb-1">{icon}</div>
      )}
      <div className="text-sm font-semibold text-gray-900 dark:text-white">{label}</div>
      {description && (
        <div className="text-[10px] text-gray-500 dark:text-gray-400 text-center mt-0.5">{description}</div>
      )}
    </button>
  );
}

// Grid of action cards
interface ActionCardGridProps {
  actions: Array<{
    id: string;
    icon: string;
    label: string;
    description?: string;
    onClick: () => void;
    disabled?: boolean;
    loading?: boolean;
    variant?: 'default' | 'primary' | 'success' | 'warning' | 'danger';
  }>;
  columns?: 2 | 3 | 4 | 5;
  className?: string;
}

const columnClasses = {
  2: 'grid-cols-2',
  3: 'grid-cols-3',
  4: 'grid-cols-2 md:grid-cols-4',
  5: 'grid-cols-2 md:grid-cols-5',
};

export function ActionCardGrid({ actions, columns = 4, className = '' }: ActionCardGridProps) {
  return (
    <div className={`grid ${columnClasses[columns]} gap-2 ${className}`}>
      {actions.map((action) => (
        <ActionCard key={action.id} {...action} />
      ))}
    </div>
  );
}
