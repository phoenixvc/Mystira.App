import type { ReactNode } from 'react';
import type { BadgeVariant, BadgeSize } from '../types';

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

