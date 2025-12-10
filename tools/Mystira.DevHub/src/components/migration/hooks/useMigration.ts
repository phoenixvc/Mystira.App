import { invoke } from '@tauri-apps/api/tauri';
import { useState, useCallback, useRef } from 'react';
import { MigrationConfig, MigrationResponse, MigrationResult, ResourceSelection, MigrationProgress } from '../types';

export function useMigration() {
  const [currentOperation, setCurrentOperation] = useState<string>('');
  const [migrationResults, setMigrationResults] = useState<MigrationResponse | null>(null);
  const [progress, setProgress] = useState<MigrationProgress | null>(null);
  const [isCancelled, setIsCancelled] = useState(false);
  const abortRef = useRef(false);

  // Connection string validation
  const validateConnectionString = (connStr: string | null | undefined, type: 'cosmos' | 'storage'): string | null => {
    if (!connStr?.trim()) {
      return `${type === 'cosmos' ? 'Cosmos DB' : 'Storage'} connection string is required`;
    }

    if (type === 'cosmos') {
      // Cosmos DB connection string format: AccountEndpoint=https://...;AccountKey=...;
      const hasEndpoint = /AccountEndpoint=https?:\/\/[^;]+/i.test(connStr);
      const hasKey = /AccountKey=[^;]+/i.test(connStr);
      
      if (!hasEndpoint || !hasKey) {
        return 'Invalid Cosmos DB connection string format. Expected format: AccountEndpoint=https://...;AccountKey=...;';
      }
    } else {
      // Storage connection string format: DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=...
      const hasAccountName = /AccountName=[^;]+/i.test(connStr);
      const hasAccountKey = /AccountKey=[^;]+/i.test(connStr);
      
      if (!hasAccountName || !hasAccountKey) {
        return 'Invalid Azure Storage connection string format. Expected format: DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;';
      }
    }

    return null;
  };

  const validateConfig = (
    config: MigrationConfig,
    selectedResources: ResourceSelection
  ): string | null => {
    // Check if any Cosmos DB migration is selected
    const needsCosmos =
      selectedResources.scenarios ||
      selectedResources.bundles ||
      selectedResources.mediaMetadata ||
      selectedResources.userProfiles ||
      selectedResources.gameSessions ||
      selectedResources.accounts ||
      selectedResources.compassTrackings ||
      selectedResources.characterMaps ||
      selectedResources.characterMapFiles ||
      selectedResources.characterMediaMetadataFiles ||
      selectedResources.avatarConfigurationFiles ||
      selectedResources.badgeConfigurations;

    // Master data seeding only needs destination connection
    const needsDestCosmos = selectedResources.masterData;

    if (needsCosmos) {
      if (!config.sourceCosmosConnection || !config.destCosmosConnection) {
        return 'Source and destination Cosmos DB connection strings are required for selected resources';
      }
      
      // Validate connection string formats
      const sourceCosmosError = validateConnectionString(config.sourceCosmosConnection, 'cosmos');
      if (sourceCosmosError) {
        return `Source Cosmos DB: ${sourceCosmosError}`;
      }
      
      const destCosmosError = validateConnectionString(config.destCosmosConnection, 'cosmos');
      if (destCosmosError) {
        return `Destination Cosmos DB: ${destCosmosError}`;
      }
      
      if (!config.sourceDatabaseName || !config.destDatabaseName) {
        return 'Source and destination database names are required';
      }
    }

    if (needsDestCosmos && !needsCosmos) {
      if (!config.destCosmosConnection) {
        return 'Destination Cosmos DB connection string is required for master data seeding';
      }
      
      const destCosmosError = validateConnectionString(config.destCosmosConnection, 'cosmos');
      if (destCosmosError) {
        return `Destination Cosmos DB: ${destCosmosError}`;
      }
      
      if (!config.destDatabaseName) {
        return 'Destination database name is required for master data seeding';
      }
    }

    if (selectedResources.blobStorage) {
      if (!config.sourceStorageConnection || !config.destStorageConnection) {
        return 'Source and destination Storage connection strings are required for blob storage migration';
      }
      
      // Validate storage connection strings
      const sourceStorageError = validateConnectionString(config.sourceStorageConnection, 'storage');
      if (sourceStorageError) {
        return `Source Storage: ${sourceStorageError}`;
      }
      
      const destStorageError = validateConnectionString(config.destStorageConnection, 'storage');
      if (destStorageError) {
        return `Destination Storage: ${destStorageError}`;
      }
      
      if (!config.containerName) {
        return 'Container name is required for blob storage migration';
      }
    }

    if (!Object.values(selectedResources).some((v) => v)) {
      return 'Please select at least one resource type to migrate';
    }

    return null;
  };

  const cancelMigration = useCallback(() => {
    abortRef.current = true;
    setIsCancelled(true);
  }, []);

  const runMigration = async (
    config: MigrationConfig,
    selectedResources: ResourceSelection
  ): Promise<MigrationResponse> => {
    // Reset state
    setCurrentOperation('');
    setMigrationResults(null);
    setIsCancelled(false);
    abortRef.current = false;

    try {
      const results: MigrationResult[] = [];
      let totalItems = 0;
      let totalSuccess = 0;
      let totalFailures = 0;

      // Build list of operations to perform
      const operations: Array<{ type: string; name: string; canParallel: boolean }> = [];

      // Core content (can run in parallel)
      if (selectedResources.scenarios) {
        operations.push({ type: 'scenarios', name: 'Migrating Scenarios...', canParallel: true });
      }
      if (selectedResources.bundles) {
        operations.push({ type: 'bundles', name: 'Migrating Content Bundles...', canParallel: true });
      }
      if (selectedResources.mediaMetadata) {
        operations.push({ type: 'media-metadata', name: 'Migrating Media Assets Metadata...', canParallel: true });
      }

      // User data (can run in parallel)
      if (selectedResources.userProfiles) {
        operations.push({ type: 'user-profiles', name: 'Migrating User Profiles...', canParallel: true });
      }
      if (selectedResources.gameSessions) {
        operations.push({ type: 'game-sessions', name: 'Migrating Game Sessions...', canParallel: true });
      }
      if (selectedResources.accounts) {
        operations.push({ type: 'accounts', name: 'Migrating Accounts...', canParallel: true });
      }
      if (selectedResources.compassTrackings) {
        operations.push({ type: 'compass-trackings', name: 'Migrating Compass Trackings...', canParallel: true });
      }

      // Reference data (can run in parallel)
      if (selectedResources.characterMaps) {
        operations.push({ type: 'character-maps', name: 'Migrating Character Maps...', canParallel: true });
      }
      if (selectedResources.characterMapFiles) {
        operations.push({ type: 'character-map-files', name: 'Migrating Character Map Files...', canParallel: true });
      }
      if (selectedResources.characterMediaMetadataFiles) {
        operations.push({ type: 'character-media-metadata-files', name: 'Migrating Character Media Files...', canParallel: true });
      }
      if (selectedResources.avatarConfigurationFiles) {
        operations.push({ type: 'avatar-configuration-files', name: 'Migrating Avatar Configurations...', canParallel: true });
      }
      if (selectedResources.badgeConfigurations) {
        operations.push({ type: 'badge-configurations', name: 'Migrating Badge Configurations...', canParallel: true });
      }

      // Master data seeding (must run sequentially, after other migrations)
      if (selectedResources.masterData) {
        operations.push({ type: 'master-data', name: 'Seeding Master Data...', canParallel: false });
      }

      // Blob storage (must run sequentially due to large file transfers)
      if (selectedResources.blobStorage) {
        operations.push({ type: 'blobs', name: 'Migrating Blob Storage Files...', canParallel: false });
      }

      const totalOperations = operations.length;
      const completedOperations: string[] = [];

      const migrateResource = async (type: string, operationName: string) => {
        if (abortRef.current) {
          throw new Error('Migration cancelled by user');
        }

        setCurrentOperation(operationName);
        setProgress({
          currentOperation: operationName,
          completedOperations: [...completedOperations],
          totalOperations,
          percentComplete: totalOperations > 0 ? Math.round((completedOperations.length / totalOperations) * 100) : 0,
          itemsProcessed: totalSuccess,
          itemsTotal: totalItems,
        });

        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: type,
          sourceCosmos: config.sourceCosmosConnection || null,
          destCosmos: config.destCosmosConnection || null,
          sourceStorage: config.sourceStorageConnection || null,
          destStorage: config.destStorageConnection || null,
          sourceDatabaseName: config.sourceDatabaseName,
          destDatabaseName: config.destDatabaseName,
          containerName: config.containerName,
          dryRun: config.dryRun || false,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }

        completedOperations.push(operationName);
        
        return response;
      };

      // Separate operations into parallel and sequential
      const parallelOps = operations.filter(op => op.canParallel);
      const sequentialOps = operations.filter(op => !op.canParallel);

      // Run parallel operations first
      if (parallelOps.length > 0) {
        const parallelPromises = parallelOps.map(op =>
          migrateResource(op.type, op.name).then(result => ({ op, result }))
        );
        
        const parallelResults = await Promise.all(parallelPromises);
        
        // Check for cancellation or failures
        if (abortRef.current) {
          throw new Error('Migration cancelled by user');
        }
      }

      // Run sequential operations one by one
      for (const op of sequentialOps) {
        if (abortRef.current) {
          throw new Error('Migration cancelled by user');
        }
        await migrateResource(op.type, op.name);
      }

      const overallSuccess = results.every((r) => r.success);
      const response: MigrationResponse = {
        success: overallSuccess,
        result: {
          overallSuccess,
          totalItems,
          totalSuccess,
          totalFailures,
          results,
        },
      };

      setMigrationResults(response);
      setCurrentOperation('');
      setProgress(null);
      return response;
    } catch (error) {
      const errorMessage = abortRef.current ? 'Migration cancelled by user' : String(error);
      const response: MigrationResponse = {
        success: false,
        error: errorMessage,
      };
      setMigrationResults(response);
      setCurrentOperation('');
      setProgress(null);
      return response;
    }
  };

  return {
    currentOperation,
    migrationResults,
    progress,
    isCancelled,
    validateConfig,
    runMigration,
    cancelMigration,
  };
}
