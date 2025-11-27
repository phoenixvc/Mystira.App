import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';

// =============================================================================
// Types
// =============================================================================

export type ButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'ghost' | 'link';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  icon?: ReactNode;
  iconPosition?: 'left' | 'right';
  fullWidth?: boolean;
  children?: ReactNode;
}

// =============================================================================
// Styles
// =============================================================================

const baseStyles = 'inline-flex items-center justify-center font-medium rounded transition-colors focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed';

const variantStyles: Record<ButtonVariant, string> = {
  primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500 dark:bg-blue-500 dark:hover:bg-blue-600',
  secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-400 dark:bg-gray-700 dark:text-gray-100 dark:hover:bg-gray-600',
  success: 'bg-green-600 text-white hover:bg-green-700 focus:ring-green-500 dark:bg-green-500 dark:hover:bg-green-600',
  danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 dark:bg-red-500 dark:hover:bg-red-600',
  warning: 'bg-yellow-500 text-black hover:bg-yellow-600 focus:ring-yellow-400 dark:bg-yellow-400 dark:hover:bg-yellow-500',
  ghost: 'bg-transparent text-gray-700 hover:bg-gray-100 focus:ring-gray-400 dark:text-gray-300 dark:hover:bg-gray-800',
  link: 'bg-transparent text-blue-600 hover:text-blue-800 hover:underline focus:ring-blue-400 dark:text-blue-400 dark:hover:text-blue-300 p-0',
};

const sizeStyles: Record<ButtonSize, string> = {
  xs: 'px-1.5 py-0.5 text-[10px] gap-1',
  sm: 'px-2 py-1 text-xs gap-1.5',
  md: 'px-3 py-1.5 text-sm gap-2',
  lg: 'px-4 py-2 text-base gap-2',
};

// =============================================================================
// Component
// =============================================================================

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'sm',
      loading = false,
      icon,
      iconPosition = 'left',
      fullWidth = false,
      disabled,
      className = '',
      children,
      ...props
    },
    ref
  ) => {
    const isDisabled = disabled || loading;

    return (
      <button
        ref={ref}
        disabled={isDisabled}
        className={`
          ${baseStyles}
          ${variantStyles[variant]}
          ${sizeStyles[size]}
          ${fullWidth ? 'w-full' : ''}
          ${className}
        `.trim().replace(/\s+/g, ' ')}
        {...props}
      >
        {loading && (
          <span className="animate-spin">⟳</span>
        )}
        {!loading && icon && iconPosition === 'left' && icon}
        {children}
        {!loading && icon && iconPosition === 'right' && icon}
      </button>
    );
  }
);

Button.displayName = 'Button';

// =============================================================================
// Icon Button
// =============================================================================

export interface IconButtonProps extends Omit<ButtonProps, 'children' | 'icon' | 'iconPosition'> {
  icon: ReactNode;
  label: string; // For accessibility
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ icon, label, size = 'sm', className = '', ...props }, ref) => {
    const iconSizeStyles: Record<ButtonSize, string> = {
      xs: 'w-5 h-5 text-[10px]',
      sm: 'w-6 h-6 text-xs',
      md: 'w-8 h-8 text-sm',
      lg: 'w-10 h-10 text-base',
    };

    return (
      <Button
        ref={ref}
        size={size}
        className={`${iconSizeStyles[size]} p-0 ${className}`}
        aria-label={label}
        title={label}
        {...props}
      >
        {icon}
      </Button>
    );
  }
);

IconButton.displayName = 'IconButton';

// =============================================================================
// Button Group
// =============================================================================

export interface ButtonGroupProps {
  children: ReactNode;
  className?: string;
}

export function ButtonGroup({ children, className = '' }: ButtonGroupProps) {
  return (
    <div className={`inline-flex rounded overflow-hidden ${className}`} role="group">
      {children}
    </div>
  );
}

// =============================================================================
// Action Card (for toolbars)
// =============================================================================

export interface ActionCardProps {
  icon: ReactNode;
  label: string;
  description?: string;
  onClick: () => void;
  disabled?: boolean;
  loading?: boolean;
  variant?: 'primary' | 'success' | 'warning' | 'danger';
  compact?: boolean;
}

const actionCardVariants = {
  primary: 'border-blue-200 dark:border-blue-800 hover:bg-blue-50 dark:hover:bg-blue-900/30',
  success: 'border-green-200 dark:border-green-800 hover:bg-green-50 dark:hover:bg-green-900/30',
  warning: 'border-yellow-200 dark:border-yellow-800 hover:bg-yellow-50 dark:hover:bg-yellow-900/30',
  danger: 'border-red-200 dark:border-red-800 hover:bg-red-50 dark:hover:bg-red-900/30',
};

export function ActionCard({
  icon,
  label,
  description,
  onClick,
  disabled = false,
  loading = false,
  variant = 'primary',
  compact = false,
}: ActionCardProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled || loading}
      className={`
        flex flex-col items-center justify-center
        ${compact ? 'px-2 py-1.5 min-w-[60px]' : 'px-3 py-2 min-w-[80px]'}
        border rounded-lg
        bg-white dark:bg-gray-800
        text-gray-900 dark:text-gray-100
        transition-colors
        disabled:opacity-50 disabled:cursor-not-allowed
        ${actionCardVariants[variant]}
      `.trim().replace(/\s+/g, ' ')}
    >
      <span className={`${compact ? 'text-base' : 'text-lg'} ${loading ? 'animate-spin' : ''}`}>
        {loading ? '⟳' : icon}
      </span>
      <span className={`${compact ? 'text-[9px]' : 'text-[10px]'} font-semibold mt-0.5`}>
        {label}
      </span>
      {description && !compact && (
        <span className="text-[8px] text-gray-500 dark:text-gray-400 mt-0.5">
          {description}
        </span>
      )}
    </button>
  );
}

// =============================================================================
// Action Card Grid
// =============================================================================

export interface ActionCardGridProps {
  actions: ActionCardProps[];
  columns?: number;
  compact?: boolean;
}

export function ActionCardGrid({ actions, columns = 4, compact = false }: ActionCardGridProps) {
  return (
    <div
      className="inline-grid gap-1"
      style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
    >
      {actions.map((action, index) => (
        <ActionCard key={index} {...action} compact={compact} />
      ))}
    </div>
  );
}

export default Button;
