
export type OperationStatus = 'success' | 'failed' | 'in_progress' | 'pending' | 'cancelled' | 'unknown';

interface OperationStatusBadgeProps {
  status: OperationStatus | string;
  showIcon?: boolean;
  size?: 'sm' | 'md' | 'lg';
}

const STATUS_CONFIG: Record<string, { 
  label: string; 
  icon: string; 
  className: string;
}> = {
  success: {
    label: 'Success',
    icon: '✓',
    className: 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300',
  },
  failed: {
    label: 'Failed',
    icon: '✗',
    className: 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300',
  },
  in_progress: {
    label: 'In Progress',
    icon: '⏳',
    className: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
  },
  pending: {
    label: 'Pending',
    icon: '⏸',
    className: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
  },
  cancelled: {
    label: 'Cancelled',
    icon: '✕',
    className: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
  },
  unknown: {
    label: 'Unknown',
    icon: '?',
    className: 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300',
  },
};

const SIZE_CLASSES = {
  sm: 'text-[10px] px-1.5 py-0.5',
  md: 'text-xs px-2 py-1',
  lg: 'text-sm px-2.5 py-1.5',
};

export function OperationStatusBadge({ 
  status, 
  showIcon = true,
  size = 'md',
}: OperationStatusBadgeProps) {
  const config = STATUS_CONFIG[status.toLowerCase()] || STATUS_CONFIG.unknown;
  const sizeClass = SIZE_CLASSES[size];

  return (
    <span 
      className={`inline-flex items-center gap-1 ${sizeClass} ${config.className} rounded-full font-medium`}
      role="status"
      aria-label={`Status: ${config.label}`}
    >
      {showIcon && <span>{config.icon}</span>}
      <span>{config.label}</span>
    </span>
  );
}

