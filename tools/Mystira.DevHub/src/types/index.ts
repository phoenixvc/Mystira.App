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
}

export interface WorkflowStatus {
  status: string;
  conclusion: string;
  workflowName: string;
  updatedAt: string;
  htmlUrl: string;
}

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
