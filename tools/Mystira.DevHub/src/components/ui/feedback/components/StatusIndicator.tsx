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

