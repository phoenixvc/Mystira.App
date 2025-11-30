// =============================================================================
// Environment Module - Consolidated exports
// =============================================================================

// Types
export type {
  Environment, EnvironmentConfig, EnvironmentHealthStatus, EnvironmentPreset, EnvironmentStatus, EnvironmentUrls, EnvironmentWarning
} from './types';

export { ENVIRONMENT_CONFIG, STATUS_INDICATORS } from './types';

// Components
export { EnvironmentBanner } from './EnvironmentBanner';
// EnvironmentContextWarning only exports a utility function, not a component
export { EnvironmentPresetSelector } from './EnvironmentPresetSelector';
export {
  DEFAULT_PRESETS,
  deletePreset,
  getAllPresets,
  getSavedPresets,
  savePreset
} from './EnvironmentPresets';
export { EnvironmentSwitcher } from './EnvironmentSwitcher';

// Utils
export { checkEnvironmentContext } from './EnvironmentContextWarning';

// Hooks
export { useEnvironmentManagement } from '../hooks/useEnvironmentManagement';

// =============================================================================
// Helper Functions
// =============================================================================

import { ENVIRONMENT_CONFIG, Environment, EnvironmentWarning, STATUS_INDICATORS, type EnvironmentHealthStatus } from './types';

/**
 * Get the display configuration for an environment
 */
export function getEnvironmentDisplay(env: Environment) {
  return ENVIRONMENT_CONFIG[env];
}

/**
 * Get the status indicator for a health status
 */
export function getStatusIndicator(status: EnvironmentHealthStatus | undefined) {
  return STATUS_INDICATORS[status || 'unknown'];
}

/**
 * Check if an environment is dangerous (prod)
 */
export function isDangerousEnvironment(env: Environment): boolean {
  return env === 'prod';
}

/**
 * Get environment badge styles
 */
export function getEnvironmentBadgeStyles(env: Environment, isActive: boolean = false): string {
  const config = ENVIRONMENT_CONFIG[env];
  if (isActive) {
    return config.activeBg;
  }
  return `bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 ${config.hoverBg}`;
}

/**
 * Format environment warning message
 */
export function formatEnvironmentWarning(warning: EnvironmentWarning): string {
  return warning.message;
}

/**
 * Get all services on a specific environment
 */
export function getServicesOnEnvironment(
  serviceEnvironments: Record<string, Environment>,
  targetEnv: Environment
): string[] {
  return Object.entries(serviceEnvironments)
    .filter(([, env]) => env === targetEnv)
    .map(([name]) => name);
}

/**
 * Count services per environment
 */
export function countServicesByEnvironment(
  serviceEnvironments: Record<string, Environment>
): Record<Environment, number> {
  const counts: Record<Environment, number> = { local: 0, dev: 0, prod: 0 };

  Object.values(serviceEnvironments).forEach(env => {
    counts[env] = (counts[env] || 0) + 1;
  });

  return counts;
}

/**
 * Generate environment summary text
 */
export function getEnvironmentSummary(serviceEnvironments: Record<string, Environment>): string {
  const counts = countServicesByEnvironment(serviceEnvironments);
  const parts: string[] = [];

  if (counts.local > 0) parts.push(`${counts.local} Local`);
  if (counts.dev > 0) parts.push(`${counts.dev} Dev`);
  if (counts.prod > 0) parts.push(`${counts.prod} Prod`);

  return parts.length > 0 ? parts.join(' | ') : 'All Local';
}
