// View/Route Constants
export const VIEWS = {
  SERVICES: 'services',
  DASHBOARD: 'dashboard',
  COSMOS: 'cosmos',
  MIGRATION: 'migration',
  INFRASTRUCTURE: 'infrastructure',
  TEST: 'test',
} as const;

export type View = typeof VIEWS[keyof typeof VIEWS];

// Infrastructure Action Constants
export const INFRASTRUCTURE_ACTIONS = {
  VALIDATE: 'validate',
  PREVIEW: 'preview',
  DEPLOY: 'deploy',
  DESTROY: 'destroy',
} as const;

export type InfrastructureAction = typeof INFRASTRUCTURE_ACTIONS[keyof typeof INFRASTRUCTURE_ACTIONS];

// Environment Constants
export const ENVIRONMENTS = {
  LOCAL: 'local',
  DEV: 'dev',
  PROD: 'prod',
} as const;

export type Environment = typeof ENVIRONMENTS[keyof typeof ENVIRONMENTS];

// Log Type Constants
export const LOG_TYPES = {
  STDOUT: 'stdout',
  STDERR: 'stderr',
  ALL: 'all',
} as const;

export type LogType = typeof LOG_TYPES[keyof typeof LOG_TYPES];

// Log Source Constants
export const LOG_SOURCES = {
  BUILD: 'build',
  RUN: 'run',
  ALL: 'all',
} as const;

export type LogSource = typeof LOG_SOURCES[keyof typeof LOG_SOURCES];

// Log Severity Constants
export const LOG_SEVERITY = {
  ALL: 'all',
  ERRORS: 'errors',
  WARNINGS: 'warnings',
  INFO: 'info',
} as const;

export type LogSeverity = typeof LOG_SEVERITY[keyof typeof LOG_SEVERITY];

// Service Status Constants
export const SERVICE_STATUS = {
  HEALTHY: 'healthy',
  UNHEALTHY: 'unhealthy',
  UNKNOWN: 'unknown',
} as const;

export type ServiceHealth = typeof SERVICE_STATUS[keyof typeof SERVICE_STATUS];

// Build Status Constants
export const BUILD_STATUS = {
  IDLE: 'idle',
  BUILDING: 'building',
  SUCCESS: 'success',
  FAILED: 'failed',
} as const;

export type BuildStatusValue = typeof BUILD_STATUS[keyof typeof BUILD_STATUS];

// Deployment Status Constants
export const DEPLOYMENT_STATUS = {
  SUCCESS: 'success',
  FAILED: 'failed',
  IN_PROGRESS: 'in_progress',
} as const;

export type DeploymentStatus = typeof DEPLOYMENT_STATUS[keyof typeof DEPLOYMENT_STATUS];

// Change Type Constants
export const CHANGE_TYPES = {
  CREATE: 'create',
  MODIFY: 'modify',
  DELETE: 'delete',
  NO_CHANGE: 'noChange',
} as const;

export type ChangeType = typeof CHANGE_TYPES[keyof typeof CHANGE_TYPES];

// Deployment Method Constants
export const DEPLOYMENT_METHODS = {
  GITHUB: 'github',
  AZURE_CLI: 'azure-cli',
} as const;

export type DeploymentMethod = typeof DEPLOYMENT_METHODS[keyof typeof DEPLOYMENT_METHODS];

// View Mode Constants
export const VIEW_MODES = {
  LOGS: 'logs',
  WEBVIEW: 'webview',
  SPLIT: 'split',
} as const;

export type ViewMode = typeof VIEW_MODES[keyof typeof VIEW_MODES];

// Connection Status Constants
export const CONNECTION_STATUS = {
  CONNECTED: 'connected',
  DISCONNECTED: 'disconnected',
  CHECKING: 'checking',
} as const;

export type ConnectionStatusValue = typeof CONNECTION_STATUS[keyof typeof CONNECTION_STATUS];

// Environment Status Constants
export const ENVIRONMENT_STATUS = {
  ONLINE: 'online',
  OFFLINE: 'offline',
  CHECKING: 'checking',
} as const;

export type EnvironmentStatusValue = typeof ENVIRONMENT_STATUS[keyof typeof ENVIRONMENT_STATUS];

// Bottom Panel Tab Constants
export const BOTTOM_PANEL_TABS = {
  OUTPUT: 'output',
  TERMINAL: 'terminal',
} as const;

export type BottomPanelTabId = typeof BOTTOM_PANEL_TABS[keyof typeof BOTTOM_PANEL_TABS];

// Storage Keys
export const STORAGE_KEYS = {
  SERVICE_ENVIRONMENTS: 'serviceEnvironments',
  SERVICE_PORTS: 'servicePorts',
  DEVHUB_LAYOUT: 'devhubLayout',
} as const;

// Event Names
export const EVENTS = {
  GLOBAL_LOG: 'global-log',
  DEPLOYMENT_LOGS: 'deployment-logs',
  INFRASTRUCTURE_PROBLEM: 'infrastructure-problem',
  NAVIGATE_TO_INFRASTRUCTURE: 'navigate-to-infrastructure',
  SERVICE_LOG: 'service-log',
} as const;

// Log Source Names (for display)
export const LOG_SOURCE_NAMES = {
  INFRASTRUCTURE: 'Infrastructure',
  SYSTEM: 'System',
} as const;

