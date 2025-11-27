import { useEffect, useState, type ReactNode } from 'react';

// =============================================================================
// Types
// =============================================================================

export type FeedbackType = 'success' | 'error' | 'warning' | 'info';

// =============================================================================
// Alert Component
// =============================================================================

export interface AlertProps {
  type: FeedbackType;
  title?: string;
  children: ReactNode;
  dismissible?: boolean;
  onDismiss?: () => void;
  icon?: ReactNode;
  compact?: boolean;
  className?: string;
}

const alertStyles: Record<FeedbackType, { bg: string; border: string; text: string; icon: string }> = {
  success: {
    bg: 'bg-green-50 dark:bg-green-900/30',
    border: 'border-green-200 dark:border-green-800',
    text: 'text-green-800 dark:text-green-200',
    icon: '✓',
  },
  error: {
    bg: 'bg-red-50 dark:bg-red-900/30',
    border: 'border-red-200 dark:border-red-800',
    text: 'text-red-800 dark:text-red-200',
    icon: '✕',
  },
  warning: {
    bg: 'bg-yellow-50 dark:bg-yellow-900/30',
    border: 'border-yellow-200 dark:border-yellow-800',
    text: 'text-yellow-800 dark:text-yellow-200',
    icon: '⚠',
  },
  info: {
    bg: 'bg-blue-50 dark:bg-blue-900/30',
    border: 'border-blue-200 dark:border-blue-800',
    text: 'text-blue-800 dark:text-blue-200',
    icon: 'ℹ',
  },
};

export function Alert({
  type,
  title,
  children,
  dismissible = false,
  onDismiss,
  icon,
  compact = false,
  className = '',
}: AlertProps) {
  const styles = alertStyles[type];

  return (
    <div
      className={`
        ${styles.bg} ${styles.border} ${styles.text}
        border rounded-lg
        ${compact ? 'px-2 py-1.5 text-xs' : 'px-3 py-2 text-sm'}
        ${className}
      `.trim().replace(/\s+/g, ' ')}
      role="alert"
    >
      <div className="flex items-start gap-2">
        <span className={compact ? 'text-sm' : 'text-base'}>{icon || styles.icon}</span>
        <div className="flex-1 min-w-0">
          {title && (
            <div className={`font-semibold ${compact ? 'text-[10px]' : 'text-xs'}`}>
              {title}
            </div>
          )}
          <div className={title ? 'mt-0.5' : ''}>{children}</div>
        </div>
        {dismissible && (
          <button
            onClick={onDismiss}
            className="text-current opacity-60 hover:opacity-100 transition-opacity"
            aria-label="Dismiss"
          >
            ✕
          </button>
        )}
      </div>
    </div>
  );
}

// =============================================================================
// Status Badge Component
// =============================================================================

export type BadgeVariant = 'default' | 'success' | 'error' | 'warning' | 'info' | 'outline';
export type BadgeSize = 'xs' | 'sm' | 'md';

export interface StatusBadgeProps {
  children: ReactNode;
  variant?: BadgeVariant;
  size?: BadgeSize;
  dot?: boolean;
  pulse?: boolean;
  className?: string;
}

const badgeVariantStyles: Record<BadgeVariant, string> = {
  default: 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300',
  success: 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300',
  error: 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300',
  warning: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
  info: 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300',
  outline: 'bg-transparent border border-current text-gray-600 dark:text-gray-400',
};

const badgeSizeStyles: Record<BadgeSize, string> = {
  xs: 'px-1 py-0.5 text-[9px]',
  sm: 'px-1.5 py-0.5 text-[10px]',
  md: 'px-2 py-1 text-xs',
};

const dotColors: Record<BadgeVariant, string> = {
  default: 'bg-gray-500',
  success: 'bg-green-500',
  error: 'bg-red-500',
  warning: 'bg-yellow-500',
  info: 'bg-blue-500',
  outline: 'bg-current',
};

export function StatusBadge({
  children,
  variant = 'default',
  size = 'sm',
  dot = false,
  pulse = false,
  className = '',
}: StatusBadgeProps) {
  return (
    <span
      className={`
        inline-flex items-center gap-1 font-semibold uppercase tracking-wide rounded
        ${badgeVariantStyles[variant]}
        ${badgeSizeStyles[size]}
        ${className}
      `.trim().replace(/\s+/g, ' ')}
    >
      {dot && (
        <span className={`w-1.5 h-1.5 rounded-full ${dotColors[variant]} ${pulse ? 'animate-pulse' : ''}`} />
      )}
      {children}
    </span>
  );
}

// =============================================================================
// Inline Status Indicator
// =============================================================================

export interface StatusIndicatorProps {
  status: 'online' | 'offline' | 'checking' | 'unknown' | 'healthy' | 'unhealthy';
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  className?: string;
}

const statusConfig = {
  online: { color: 'bg-green-500', label: 'Online', emoji: '🟢' },
  offline: { color: 'bg-red-500', label: 'Offline', emoji: '🔴' },
  checking: { color: 'bg-yellow-500', label: 'Checking', emoji: '🟡' },
  unknown: { color: 'bg-gray-400', label: 'Unknown', emoji: '⚪' },
  healthy: { color: 'bg-green-500', label: 'Healthy', emoji: '💚' },
  unhealthy: { color: 'bg-red-500', label: 'Unhealthy', emoji: '💔' },
};

const indicatorSizes = {
  sm: 'w-1.5 h-1.5',
  md: 'w-2 h-2',
  lg: 'w-2.5 h-2.5',
};

export function StatusIndicator({
  status,
  size = 'sm',
  showLabel = false,
  className = '',
}: StatusIndicatorProps) {
  const config = statusConfig[status];

  return (
    <span className={`inline-flex items-center gap-1 ${className}`} title={config.label}>
      <span
        className={`${indicatorSizes[size]} ${config.color} rounded-full ${
          status === 'checking' ? 'animate-pulse' : ''
        }`}
      />
      {showLabel && <span className="text-xs text-gray-600 dark:text-gray-400">{config.label}</span>}
    </span>
  );
}

// =============================================================================
// Toast System (Enhanced)
// =============================================================================

export interface Toast {
  id: string;
  message: string;
  type: FeedbackType;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface ToastItemProps {
  toast: Toast;
  onClose: (id: string) => void;
}

const toastStyles: Record<FeedbackType, string> = {
  success: 'bg-green-600 dark:bg-green-500',
  error: 'bg-red-600 dark:bg-red-500',
  warning: 'bg-yellow-500 dark:bg-yellow-400 text-black',
  info: 'bg-blue-600 dark:bg-blue-500',
};

const toastIcons: Record<FeedbackType, string> = {
  success: '✓',
  error: '✕',
  warning: '⚠',
  info: 'ℹ',
};

function ToastItem({ toast, onClose }: ToastItemProps) {
  useEffect(() => {
    if (toast.duration !== 0) {
      const timer = setTimeout(() => {
        onClose(toast.id);
      }, toast.duration || 5000);
      return () => clearTimeout(timer);
    }
  }, [toast, onClose]);

  return (
    <div
      className={`
        ${toastStyles[toast.type]}
        text-white px-4 py-3 rounded-lg shadow-lg mb-2
        flex items-center justify-between
        min-w-[300px] max-w-[500px]
        animate-slide-in
      `.trim().replace(/\s+/g, ' ')}
      role="alert"
    >
      <div className="flex items-center gap-2">
        <span className="text-lg">{toastIcons[toast.type]}</span>
        <span className="text-sm">{toast.message}</span>
      </div>
      <div className="flex items-center gap-2 ml-4">
        {toast.action && (
          <button
            onClick={toast.action.onClick}
            className="text-sm font-medium underline hover:no-underline"
          >
            {toast.action.label}
          </button>
        )}
        <button
          onClick={() => onClose(toast.id)}
          className="text-white hover:text-gray-200 font-bold text-lg leading-none"
          aria-label="Close"
        >
          ×
        </button>
      </div>
    </div>
  );
}

export interface ToastContainerProps {
  toasts: Toast[];
  onClose: (id: string) => void;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
}

const positionStyles = {
  'top-right': 'top-4 right-4',
  'top-left': 'top-4 left-4',
  'bottom-right': 'bottom-4 right-4',
  'bottom-left': 'bottom-4 left-4',
};

export function ToastContainer({ toasts, onClose, position = 'top-right' }: ToastContainerProps) {
  const isTop = position.startsWith('top');

  return (
    <div className={`fixed ${positionStyles[position]} z-50 flex ${isTop ? 'flex-col-reverse' : 'flex-col'}`}>
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onClose={onClose} />
      ))}
    </div>
  );
}

// =============================================================================
// Toast Hook
// =============================================================================

export function useToast() {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = (
    message: string,
    type: FeedbackType = 'info',
    options?: { duration?: number; action?: Toast['action'] }
  ): Toast => {
    const toast: Toast = {
      id: `toast-${Date.now()}-${Math.random().toString(36).slice(2)}`,
      message,
      type,
      duration: options?.duration ?? 5000,
      action: options?.action,
    };
    setToasts((prev) => [...prev, toast]);
    return toast;
  };

  const dismissToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  const clearToasts = () => {
    setToasts([]);
  };

  return {
    toasts,
    showToast,
    dismissToast,
    clearToasts,
    success: (msg: string, opts?: { duration?: number }) => showToast(msg, 'success', opts),
    error: (msg: string, opts?: { duration?: number }) => showToast(msg, 'error', opts),
    warning: (msg: string, opts?: { duration?: number }) => showToast(msg, 'warning', opts),
    info: (msg: string, opts?: { duration?: number }) => showToast(msg, 'info', opts),
  };
}

// =============================================================================
// Progress Bar
// =============================================================================

export interface ProgressBarProps {
  value: number; // 0-100
  max?: number;
  variant?: 'default' | 'success' | 'warning' | 'error';
  size?: 'xs' | 'sm' | 'md';
  showLabel?: boolean;
  className?: string;
}

const progressVariants = {
  default: 'bg-blue-600 dark:bg-blue-500',
  success: 'bg-green-600 dark:bg-green-500',
  warning: 'bg-yellow-500 dark:bg-yellow-400',
  error: 'bg-red-600 dark:bg-red-500',
};

const progressSizes = {
  xs: 'h-1',
  sm: 'h-1.5',
  md: 'h-2',
};

export function ProgressBar({
  value,
  max = 100,
  variant = 'default',
  size = 'sm',
  showLabel = false,
  className = '',
}: ProgressBarProps) {
  const percentage = Math.min(100, Math.max(0, (value / max) * 100));

  return (
    <div className={`w-full ${className}`}>
      <div className={`w-full bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden ${progressSizes[size]}`}>
        <div
          className={`${progressVariants[variant]} ${progressSizes[size]} rounded-full transition-all duration-300`}
          style={{ width: `${percentage}%` }}
        />
      </div>
      {showLabel && (
        <span className="text-[10px] text-gray-600 dark:text-gray-400 mt-0.5">
          {Math.round(percentage)}%
        </span>
      )}
    </div>
  );
}

export default Alert;
