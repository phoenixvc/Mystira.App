import { type ReactNode } from 'react';

// =============================================================================
// Spinner Component
// =============================================================================

export type SpinnerSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type SpinnerVariant = 'default' | 'primary' | 'white';

export interface SpinnerProps {
  size?: SpinnerSize;
  variant?: SpinnerVariant;
  className?: string;
  label?: string;
}

const spinnerSizes: Record<SpinnerSize, string> = {
  xs: 'w-3 h-3 border',
  sm: 'w-4 h-4 border-2',
  md: 'w-6 h-6 border-2',
  lg: 'w-8 h-8 border-2',
  xl: 'w-12 h-12 border-4',
};

const spinnerVariants: Record<SpinnerVariant, string> = {
  default: 'border-gray-300 border-t-gray-600 dark:border-gray-600 dark:border-t-gray-300',
  primary: 'border-blue-200 border-t-blue-600 dark:border-blue-800 dark:border-t-blue-400',
  white: 'border-white/30 border-t-white',
};

export function Spinner({ size = 'md', variant = 'default', className = '', label }: SpinnerProps) {
  return (
    <div className={`inline-flex items-center gap-2 ${className}`} role="status" aria-label={label || 'Loading'}>
      <div
        className={`
          ${spinnerSizes[size]}
          ${spinnerVariants[variant]}
          rounded-full animate-spin
        `.trim().replace(/\s+/g, ' ')}
      />
      {label && <span className="text-sm text-gray-600 dark:text-gray-400">{label}</span>}
    </div>
  );
}

// =============================================================================
// Loading Dots
// =============================================================================

export interface LoadingDotsProps {
  size?: 'sm' | 'md';
  className?: string;
}

export function LoadingDots({ size = 'md', className = '' }: LoadingDotsProps) {
  const dotSize = size === 'sm' ? 'w-1 h-1' : 'w-1.5 h-1.5';

  return (
    <span className={`inline-flex items-center gap-1 ${className}`}>
      <span className={`${dotSize} bg-current rounded-full animate-bounce`} style={{ animationDelay: '0ms' }} />
      <span className={`${dotSize} bg-current rounded-full animate-bounce`} style={{ animationDelay: '150ms' }} />
      <span className={`${dotSize} bg-current rounded-full animate-bounce`} style={{ animationDelay: '300ms' }} />
    </span>
  );
}

// =============================================================================
// Loading Overlay
// =============================================================================

export interface LoadingOverlayProps {
  visible: boolean;
  message?: string;
  blur?: boolean;
  className?: string;
}

export function LoadingOverlay({ visible, message, blur = true, className = '' }: LoadingOverlayProps) {
  if (!visible) return null;

  return (
    <div
      className={`
        absolute inset-0 z-50
        flex flex-col items-center justify-center
        bg-white/80 dark:bg-gray-900/80
        ${blur ? 'backdrop-blur-sm' : ''}
        ${className}
      `.trim().replace(/\s+/g, ' ')}
    >
      <Spinner size="lg" variant="primary" />
      {message && (
        <p className="mt-3 text-sm text-gray-600 dark:text-gray-400">{message}</p>
      )}
    </div>
  );
}

// =============================================================================
// Skeleton Components
// =============================================================================

export interface SkeletonProps {
  className?: string;
  animate?: boolean;
}

export function Skeleton({ className = '', animate = true }: SkeletonProps) {
  return (
    <div
      className={`
        bg-gray-200 dark:bg-gray-700 rounded
        ${animate ? 'animate-pulse' : ''}
        ${className}
      `.trim().replace(/\s+/g, ' ')}
    />
  );
}

export interface SkeletonTextProps {
  lines?: number;
  className?: string;
}

export function SkeletonText({ lines = 3, className = '' }: SkeletonTextProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton
          key={i}
          className={`h-3 ${i === lines - 1 ? 'w-3/4' : 'w-full'}`}
        />
      ))}
    </div>
  );
}

export interface SkeletonCardProps {
  hasImage?: boolean;
  className?: string;
}

export function SkeletonCard({ hasImage = false, className = '' }: SkeletonCardProps) {
  return (
    <div className={`p-4 border border-gray-200 dark:border-gray-700 rounded-lg ${className}`}>
      {hasImage && <Skeleton className="h-32 w-full mb-4" />}
      <Skeleton className="h-4 w-3/4 mb-2" />
      <Skeleton className="h-3 w-1/2 mb-4" />
      <SkeletonText lines={2} />
    </div>
  );
}

export interface SkeletonTableProps {
  rows?: number;
  columns?: number;
  className?: string;
}

export function SkeletonTable({ rows = 5, columns = 4, className = '' }: SkeletonTableProps) {
  return (
    <div className={`space-y-2 ${className}`}>
      {/* Header */}
      <div className="flex gap-4 pb-2 border-b border-gray-200 dark:border-gray-700">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="h-4 flex-1" />
        ))}
      </div>
      {/* Rows */}
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div key={rowIndex} className="flex gap-4 py-2">
          {Array.from({ length: columns }).map((_, colIndex) => (
            <Skeleton key={colIndex} className="h-3 flex-1" />
          ))}
        </div>
      ))}
    </div>
  );
}

// =============================================================================
// Loading State Wrapper
// =============================================================================

export interface LoadingStateProps {
  loading: boolean;
  error?: string | null;
  empty?: boolean;
  emptyMessage?: string;
  emptyIcon?: string;
  emptyTitle?: string;
  emptyAction?: {
    label: string;
    onClick: () => void;
  };
  errorAction?: {
    label: string;
    onClick: () => void;
  };
  skeleton?: ReactNode;
  children: ReactNode;
  className?: string;
}

export function LoadingState({
  loading,
  error,
  empty = false,
  emptyMessage = 'No data available',
  emptyIcon = '📭',
  emptyTitle,
  emptyAction,
  errorAction,
  skeleton,
  children,
  className = '',
}: LoadingStateProps) {
  if (loading) {
    return (
      <div className={className}>
        {skeleton || (
          <div className="flex flex-col items-center justify-center py-8">
            <Spinner size="lg" variant="primary" />
            <p className="mt-3 text-sm text-gray-500 dark:text-gray-400">Loading...</p>
          </div>
        )}
      </div>
    );
  }

  if (error) {
    return (
      <div className={`flex flex-col items-center justify-center py-8 ${className}`}>
        <div className="text-red-500 text-3xl mb-2">⚠</div>
        <p className="text-sm text-red-600 dark:text-red-400 text-center max-w-md">{error}</p>
        {errorAction && (
          <button
            onClick={errorAction.onClick}
            className="mt-3 px-3 py-1.5 text-sm bg-red-600 text-white rounded hover:bg-red-700 transition-colors"
          >
            {errorAction.label}
          </button>
        )}
      </div>
    );
  }

  if (empty) {
    return (
      <div className={`flex flex-col items-center justify-center py-8 ${className}`}>
        <div className="text-gray-400 text-4xl mb-3">{emptyIcon}</div>
        {emptyTitle && (
          <h3 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-1">{emptyTitle}</h3>
        )}
        <p className="text-sm text-gray-500 dark:text-gray-400 text-center max-w-md">{emptyMessage}</p>
        {emptyAction && (
          <button
            onClick={emptyAction.onClick}
            className="mt-4 px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
          >
            {emptyAction.label}
          </button>
        )}
      </div>
    );
  }

  return <>{children}</>;
}

// =============================================================================
// Inline Loading Text
// =============================================================================

export interface LoadingTextProps {
  text?: string;
  className?: string;
}

export function LoadingText({ text = 'Loading', className = '' }: LoadingTextProps) {
  return (
    <span className={`inline-flex items-center gap-1.5 text-gray-600 dark:text-gray-400 ${className}`}>
      <span className="animate-spin text-sm">⟳</span>
      <span>{text}</span>
      <LoadingDots size="sm" />
    </span>
  );
}

export default Spinner;
