// Core API Response Types
export interface CommandResponse<T = unknown> {
  success: boolean;
  result?: T;
  message?: string;
  error?: string;
}

// Connection Types
export interface ConnectionStatus {
  name: string;
  type: 'cosmos' | 'storage' | 'azurecli' | 'githubcli';
  status: 'connected' | 'disconnected' | 'checking';
  icon: string;
  details?: string;
  error?: string;
}

export interface ConnectionTestResult {
  connected: boolean;
  accountName?: string;
  user?: string;
  subscription?: string;
  consistencyLevel?: string;
  regions?: string[];
  sku?: string;
  status?: string;
}

// Azure Resource Types
export interface AzureResource {
  id: string;
  name: string;
  type: string;
  location: string;
  resourceGroup: string;
  sku?: {
    name: string;
    tier?: string;
  };
  kind?: string;
  tags?: Record<string, string>;
}

export interface AzureResourceMapped {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'unknown';
  region: string;
  costToday: number;
  lastUpdated: string;
  properties: Record<string, string>;
}

// GitHub Deployment Types
export interface GitHubWorkflowRun {
  id: number;
  name: string;
  display_title: string;
  status: string;
  conclusion: string | null;
  created_at: string;
  updated_at: string;
  html_url: string;
  path?: string;
  actor?: {
    login: string;
    avatar_url?: string;
  };
}

export interface Deployment {
  id: string;
  timestamp: string;
  action: 'deploy' | 'validate' | 'preview' | 'destroy';
  status: 'success' | 'failed' | 'in_progress';
  duration: string;
  resourcesAffected?: number;
  user: string;
  message: string;
  githubUrl?: string;
}

// Recent Operations Types
export interface RecentOperation {
  id: string;
  type: string;
  title: string;
  timestamp: string;
  status: 'success' | 'failed' | 'in_progress';
  details?: string;
}

// Quick Action Types
export interface QuickAction {
  id: string;
  title: string;
  description: string;
  icon: string;
  action: () => void;
  color: string;
}

// Infrastructure Types
export interface WhatIfChange {
  resourceType: string;
  resourceName: string;
  changeType: 'create' | 'modify' | 'delete' | 'noChange';
  changes?: string[];
  selected?: boolean; // For resource selection
  resourceId?: string; // Full Azure resource ID
  resourceGroup?: string; // Target resource group for this resource
}

export interface ResourceGroupConvention {
  pattern: string; // e.g., "mys-{env}-mystira-rg-{region}"
  defaultResourceGroup: string; // e.g., "mys-dev-mystira-rg-euw"
  resourceTypeMappings?: Record<string, string>; // e.g., { "Microsoft.Storage/storageAccounts": "mys-dev-storage-rg-euw" }
  environment?: string; // e.g., "dev", "staging", "prod"
  region?: string; // e.g., "euw", "eus", "san"
  projectName?: string; // e.g., "mystira"
}

export interface TemplateConfig {
  id: string;
  name: string;
  file: string;
  description: string;
  selected: boolean;
  resourceGroup: string;
  parameters: Record<string, any>;
}

export interface ProjectInfo {
  id: string;
  name: string;
  type: 'api' | 'admin-api' | 'pwa' | 'service';
  description: string;
  infrastructure: {
    storage: boolean;
    cosmos: boolean;
    appService: boolean;
    keyVault: boolean;
  };
  templateIds: string[];
}

export const DEFAULT_PROJECTS: ProjectInfo[] = [
  {
    id: 'mystira-api',
    name: 'Mystira.App.Api',
    type: 'api',
    description: 'Main REST API for client applications',
    infrastructure: {
      storage: true,
      cosmos: true,
      appService: true,
      keyVault: false,
    },
    templateIds: ['storage', 'cosmos', 'appservice'],
  },
  {
    id: 'mystira-admin-api',
    name: 'Mystira.App.Admin.Api',
    type: 'admin-api',
    description: 'Administrative API for content management',
    infrastructure: {
      storage: true,
      cosmos: true,
      appService: true,
      keyVault: false,
    },
    templateIds: ['storage', 'cosmos', 'appservice'],
  },
  {
    id: 'mystira-pwa',
    name: 'Mystira.App.PWA',
    type: 'pwa',
    description: 'Progressive Web Application frontend',
    infrastructure: {
      storage: false,
      cosmos: false,
      appService: true,
      keyVault: false,
    },
    templateIds: ['appservice'],
  },
];

export interface WorkflowStatus {
  status: string;
  conclusion: string;
  workflowName: string;
  updatedAt: string;
  htmlUrl: string;
}

// Cosmos DB Warning Types (for Azure what-if preview limitations)
export interface CosmosWarning {
  type: 'cosmos-whatif';
  message: string;
  details: string;
  affectedResources: string[];
  dismissed: boolean;
}

// Storage Account Conflict Warning (resource exists in another resource group)
export interface StorageAccountConflictWarning {
  type: 'storage-account-conflict';
  message: string;
  details: string;
  storageAccountName: string;
  currentResourceGroup?: string;  // The resource group where the account currently exists
  dismissed: boolean;
}

// Re-export constants for convenience
export * from './constants';

// Settings Types
export interface Settings {
  theme: 'light' | 'dark' | 'auto';
  defaultExportPath: string;
  defaultLogsPath: string;
  notificationsEnabled: boolean;
  notificationSound: boolean;
  logLevel: 'error' | 'warn' | 'info' | 'debug';
  autoUpdate: boolean;
  cacheEnabled: boolean;
  cacheDuration: number;
}

// Tauri Command Types
export interface TauriCommandParams {
  connectionType?: string;
  connectionString?: string | null;
  subscriptionId?: string | null;
  resourceGroup?: string | null;
  repository?: string;
  limit?: number;
  workflowFile?: string;
  confirm?: boolean;
}

// Component Props Types
export interface DashboardProps {
  onNavigate: (view: string) => void;
}

export interface ResourceGridProps {
  resources: AzureResourceMapped[];
  onRefresh: () => void;
}

export interface DeploymentHistoryProps {
  events: Deployment[];
}

export interface BicepFile {
  name: string;
  path: string;
  type: 'file' | 'directory';
  children?: BicepFile[];
}
