export interface EnvironmentPreset {
  id: string;
  name: string;
  description: string;
  cosmosAccountName: string;
  // Resource group and storage are auto-discovered from Cosmos account
  isLegacy?: boolean;
  defaultDatabaseName?: string;
}

export const ENVIRONMENT_PRESETS: EnvironmentPreset[] = [
  {
    id: 'old-dev',
    name: 'Old Dev Environment',
    description: 'Legacy dev environment - resource group auto-discovered',
    cosmosAccountName: 'dev-san-cosmos-mystira',
    isLegacy: true,
    defaultDatabaseName: 'MystiraDb',
  },
  {
    id: 'old-prod',
    name: 'Old Production Environment',
    description: 'Legacy prod environment - resource group auto-discovered',
    cosmosAccountName: 'prodwusappmystiracosmos',
    isLegacy: true,
    defaultDatabaseName: 'MystiraDb',
  },
  {
    id: 'new-dev',
    name: 'New Dev Environment',
    description: 'Current development environment - resource group auto-discovered',
    cosmosAccountName: 'mys-dev-mystira-cosmos-san',
    isLegacy: false,
    defaultDatabaseName: 'MystiraAppDb',
  },
  {
    id: 'new-staging',
    name: 'New Staging Environment',
    description: 'Current staging environment - resource group auto-discovered',
    cosmosAccountName: 'mys-staging-mystira-cosmos-san',
    isLegacy: false,
    defaultDatabaseName: 'MystiraAppDb',
  },
  {
    id: 'new-prod',
    name: 'New Production Environment',
    description: 'Current production environment - resource group auto-discovered',
    cosmosAccountName: 'mys-prod-mystira-cosmos-san',
    isLegacy: false,
    defaultDatabaseName: 'MystiraAppDb',
  },
  {
    id: 'custom',
    name: 'Custom',
    description: 'Enter connection strings manually',
    cosmosAccountName: '',
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
  sourceDatabaseName: string;
  destDatabaseName: string;
  containerName: string;
  dryRun: boolean;
}

export interface ResourceSelection {
  // Core content
  scenarios: boolean;
  bundles: boolean;
  mediaMetadata: boolean;
  // User data
  userProfiles: boolean;
  gameSessions: boolean;
  accounts: boolean;
  compassTrackings: boolean;
  // Reference data
  characterMaps: boolean;
  characterMapFiles: boolean;
  characterMediaMetadataFiles: boolean;
  avatarConfigurationFiles: boolean;
  badgeConfigurations: boolean;
  // Master data (seeding)
  masterData: boolean;
  // Storage
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

