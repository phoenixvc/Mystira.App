// =============================================================================
// Environment Types
// =============================================================================

export type Environment = 'local' | 'dev' | 'prod';

export type EnvironmentHealthStatus = 'online' | 'offline' | 'checking' | 'unknown';

export interface EnvironmentUrls {
  dev?: string;
  prod?: string;
}

export interface EnvironmentStatus {
  dev?: EnvironmentHealthStatus;
  prod?: EnvironmentHealthStatus;
}

export interface EnvironmentPreset {
  id: string;
  name: string;
  description: string;
  environments: Record<string, Environment>;
  isDefault?: boolean;
}

export interface EnvironmentConfig {
  serviceName: string;
  environment: Environment;
  urls: EnvironmentUrls;
  status: EnvironmentStatus;
}

// =============================================================================
// Environment Context Warning
// =============================================================================

export interface EnvironmentWarning {
  shouldWarn: boolean;
  message: string;
  severity: 'warning' | 'danger';
}

// =============================================================================
// Environment Icons/Labels
// =============================================================================

export const ENVIRONMENT_CONFIG: Record<Environment, {
  icon: string;
  label: string;
  color: string;
  bgColor: string;
  textColor: string;
  hoverBg: string;
  activeBg: string;
}> = {
  local: {
    icon: '🏠',
    label: 'Local',
    color: 'green',
    bgColor: 'bg-green-500',
    textColor: 'text-green-600 dark:text-green-400',
    hoverBg: 'hover:bg-green-50 dark:hover:bg-green-900/20',
    activeBg: 'bg-green-500 text-white',
  },
  dev: {
    icon: '🧪',
    label: 'Dev',
    color: 'blue',
    bgColor: 'bg-blue-500',
    textColor: 'text-blue-600 dark:text-blue-400',
    hoverBg: 'hover:bg-blue-50 dark:hover:bg-blue-900/20',
    activeBg: 'bg-blue-500 text-white',
  },
  prod: {
    icon: '⚠️',
    label: 'Prod',
    color: 'red',
    bgColor: 'bg-red-600',
    textColor: 'text-red-600 dark:text-red-400',
    hoverBg: 'hover:bg-red-50 dark:hover:bg-red-900/20',
    activeBg: 'bg-red-600 text-white animate-pulse',
  },
};

export const STATUS_INDICATORS: Record<EnvironmentHealthStatus, { emoji: string; label: string; color: string }> = {
  online: { emoji: '🟢', label: 'Online', color: 'text-green-500' },
  offline: { emoji: '🔴', label: 'Offline', color: 'text-red-500' },
  checking: { emoji: '🟡', label: 'Checking', color: 'text-yellow-500' },
  unknown: { emoji: '⚪', label: 'Unknown', color: 'text-gray-400' },
};
