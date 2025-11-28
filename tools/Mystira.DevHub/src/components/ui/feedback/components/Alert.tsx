import type { ReactNode } from 'react';
import type { FeedbackType } from '../types';

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

