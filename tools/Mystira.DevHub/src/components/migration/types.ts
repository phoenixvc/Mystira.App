export interface EnvironmentPreset {
  id: string;
  name: string;
  description: string;
  resourceGroup: string;
  cosmosAccountName: string;
  storageAccountName: string;
  isLegacy?: boolean;
}

export const ENVIRONMENT_PRESETS: EnvironmentPreset[] = [
  {
    id: 'old-dev',
    name: 'Old Dev Environment',
    description: 'Legacy development environment (before infrastructure migration)',
    resourceGroup: 'mystira-dev-rg',
    cosmosAccountName: 'mystira-dev-cosmos',
    storageAccountName: 'mystiradevstore',
    isLegacy: true,
  },
  {
    id: 'old-staging',
    name: 'Old Staging Environment',
    description: 'Legacy staging environment',
    resourceGroup: 'mystira-staging-rg',
    cosmosAccountName: 'mystira-staging-cosmos',
    storageAccountName: 'mystirastagingstore',
    isLegacy: true,
  },
  {
    id: 'old-prod',
    name: 'Old Production Environment',
    description: 'Legacy production environment',
    resourceGroup: 'mystira-prod-rg',
    cosmosAccountName: 'mystira-prod-cosmos',
    storageAccountName: 'mystiraprodstore',
    isLegacy: true,
  },
  {
    id: 'new-dev',
    name: 'New Dev Environment',
    description: 'Current development environment (mys-dev-mystira-rg-san)',
    resourceGroup: 'mys-dev-mystira-rg-san',
    cosmosAccountName: 'mys-dev-mystira-cosmos-san',
    storageAccountName: 'mysdevmystirastsan',
    isLegacy: false,
  },
  {
    id: 'new-staging',
    name: 'New Staging Environment',
    description: 'Current staging environment (mys-staging-mystira-rg-san)',
    resourceGroup: 'mys-staging-mystira-rg-san',
    cosmosAccountName: 'mys-staging-mystira-cosmos-san',
    storageAccountName: 'mysstagingmystirastsan',
    isLegacy: false,
  },
  {
    id: 'new-prod',
    name: 'New Production Environment',
    description: 'Current production environment (mys-prod-mystira-rg-san)',
    resourceGroup: 'mys-prod-mystira-rg-san',
    cosmosAccountName: 'mys-prod-mystira-cosmos-san',
    storageAccountName: 'mysprodmystirastsan',
    isLegacy: false,
  },
  {
    id: 'custom',
    name: 'Custom',
    description: 'Enter connection strings manually',
    resourceGroup: '',
    cosmosAccountName: '',
    storageAccountName: '',
    isLegacy: false,
  },
];

export interface MigrationConfig {
  sourceEnvironment: string;
  destEnvironment: string;
  sourceCosmosConnection: string;
  destCosmosConnection: string;
  sourceStorageConnection: string;
  destStorageConnection: string;
  databaseName: string;
  containerName: string;
}

export interface ResourceSelection {
  scenarios: boolean;
  bundles: boolean;
  mediaMetadata: boolean;
  blobStorage: boolean;
}

export interface MigrationResult {
  success: boolean;
  totalItems: number;
  successCount: number;
  failureCount: number;
  duration: string;
  errors: string[];
}

export interface MigrationResponse {
  success: boolean;
  result?: {
    overallSuccess: boolean;
    totalItems: number;
    totalSuccess: number;
    totalFailures: number;
    results: MigrationResult[];
  };
  message?: string;
  error?: string;
}

export type MigrationStep = 'configure' | 'select' | 'running' | 'complete';

