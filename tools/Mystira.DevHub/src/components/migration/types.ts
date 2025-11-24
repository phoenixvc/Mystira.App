export interface MigrationConfig {
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

