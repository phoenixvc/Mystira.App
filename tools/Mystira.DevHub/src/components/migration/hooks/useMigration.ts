import { invoke } from '@tauri-apps/api/tauri';
import { useState, useRef, useCallback } from 'react';
import { MigrationConfig, MigrationResponse, MigrationResult, ResourceSelection } from '../types';

export interface MigrationProgress {
  currentOperation: string;
  completedOperations: string[];
  totalOperations: number;
  percentComplete: number;
  itemsProcessed: number;
  itemsTotal: number;
}

export function useMigration() {
  const [progress, setProgress] = useState<MigrationProgress>({
    currentOperation: '',
    completedOperations: [],
    totalOperations: 0,
    percentComplete: 0,
    itemsProcessed: 0,
    itemsTotal: 0,
  });
  const [migrationResults, setMigrationResults] = useState<MigrationResponse | null>(null);
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

  // Get list of operations to run based on selected resources
  const getOperations = (selectedResources: ResourceSelection): Array<{ type: string; name: string; parallel?: boolean }> => {
    const ops: Array<{ type: string; name: string; parallel?: boolean }> = [];

    // Core content - can run in parallel
    if (selectedResources.scenarios) ops.push({ type: 'scenarios', name: 'Scenarios', parallel: true });
    if (selectedResources.bundles) ops.push({ type: 'bundles', name: 'Content Bundles', parallel: true });
    if (selectedResources.mediaMetadata) ops.push({ type: 'media-metadata', name: 'Media Assets', parallel: true });

    // User data - can run in parallel
    if (selectedResources.userProfiles) ops.push({ type: 'user-profiles', name: 'User Profiles', parallel: true });
    if (selectedResources.gameSessions) ops.push({ type: 'game-sessions', name: 'Game Sessions', parallel: true });
    if (selectedResources.accounts) ops.push({ type: 'accounts', name: 'Accounts', parallel: true });
    if (selectedResources.compassTrackings) ops.push({ type: 'compass-trackings', name: 'Compass Trackings', parallel: true });

    // Reference data - can run in parallel
    if (selectedResources.characterMaps) ops.push({ type: 'character-maps', name: 'Character Maps', parallel: true });
    if (selectedResources.characterMapFiles) ops.push({ type: 'character-map-files', name: 'Character Map Files', parallel: true });
    if (selectedResources.characterMediaMetadataFiles) ops.push({ type: 'character-media-metadata-files', name: 'Character Media Files', parallel: true });
    if (selectedResources.avatarConfigurationFiles) ops.push({ type: 'avatar-configuration-files', name: 'Avatar Configurations', parallel: true });
    if (selectedResources.badgeConfigurations) ops.push({ type: 'badge-configurations', name: 'Badge Configurations', parallel: true });

    // Master data seeding - run after other migrations
    if (selectedResources.masterData) ops.push({ type: 'master-data', name: 'Master Data', parallel: false });

    // Blob storage - can run in parallel with others but typically large
    if (selectedResources.blobStorage) ops.push({ type: 'blobs', name: 'Blob Storage', parallel: false });

    return ops;
  };

  const cancelMigration = useCallback(() => {
    abortRef.current = true;
    setIsCancelled(true);
  }, []);

  const runMigration = async (
    config: MigrationConfig,
    selectedResources: ResourceSelection,
    useParallel: boolean = true
  ): Promise<MigrationResponse> => {
    abortRef.current = false;
    setIsCancelled(false);
    setMigrationResults(null);

    const operations = getOperations(selectedResources);

    setProgress({
      currentOperation: 'Starting migration...',
      completedOperations: [],
      totalOperations: operations.length,
      percentComplete: 0,
      itemsProcessed: 0,
      itemsTotal: 0,
    });

    try {
      const results: MigrationResult[] = [];
      let totalItems = 0;
      let totalSuccess = 0;
      let totalFailures = 0;
      const completedOps: string[] = [];

      const migrateResource = async (type: string, operationName: string): Promise<MigrationResult | null> => {
        if (abortRef.current) {
          return null;
        }

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

        if (response.result?.results && response.result.results.length > 0) {
          return response.result.results[0];
        }
        return null;
      };

      if (useParallel) {
        // Group operations: parallel-safe vs sequential
        const parallelOps = operations.filter(op => op.parallel);
        const sequentialOps = operations.filter(op => !op.parallel);

        // Run parallel operations
        if (parallelOps.length > 0 && !abortRef.current) {
          setProgress(prev => ({
            ...prev,
            currentOperation: `Running ${parallelOps.length} migrations in parallel...`,
          }));

          const parallelPromises = parallelOps.map(op =>
            migrateResource(op.type, op.name).then(result => ({ op, result }))
          );

          const parallelResults = await Promise.all(parallelPromises);

          for (const { op, result } of parallelResults) {
            if (result) {
              results.push(result);
              totalItems += result.totalItems;
              totalSuccess += result.successCount;
              totalFailures += result.failureCount;
            }
            completedOps.push(op.name);
          }

          setProgress(prev => ({
            ...prev,
            completedOperations: [...completedOps],
            percentComplete: Math.round((completedOps.length / operations.length) * 100),
            itemsProcessed: totalSuccess + totalFailures,
            itemsTotal: totalItems,
          }));
        }

        // Run sequential operations
        for (const op of sequentialOps) {
          if (abortRef.current) break;

          setProgress(prev => ({
            ...prev,
            currentOperation: `Migrating ${op.name}...`,
          }));

          const result = await migrateResource(op.type, op.name);
          if (result) {
            results.push(result);
            totalItems += result.totalItems;
            totalSuccess += result.successCount;
            totalFailures += result.failureCount;
          }
          completedOps.push(op.name);

          setProgress(prev => ({
            ...prev,
            completedOperations: [...completedOps],
            percentComplete: Math.round((completedOps.length / operations.length) * 100),
            itemsProcessed: totalSuccess + totalFailures,
            itemsTotal: totalItems,
          }));
        }
      } else {
        // Sequential execution
        for (const op of operations) {
          if (abortRef.current) break;

          setProgress(prev => ({
            ...prev,
            currentOperation: `Migrating ${op.name}...`,
          }));

          const result = await migrateResource(op.type, op.name);
          if (result) {
            results.push(result);
            totalItems += result.totalItems;
            totalSuccess += result.successCount;
            totalFailures += result.failureCount;
          }
          completedOps.push(op.name);

          setProgress(prev => ({
            ...prev,
            completedOperations: [...completedOps],
            percentComplete: Math.round((completedOps.length / operations.length) * 100),
            itemsProcessed: totalSuccess + totalFailures,
            itemsTotal: totalItems,
          }));
        }
      }

      if (abortRef.current) {
        const response: MigrationResponse = {
          success: false,
          error: 'Migration cancelled by user',
          result: {
            overallSuccess: false,
            totalItems,
            totalSuccess,
            totalFailures,
            results,
          },
        };
        setMigrationResults(response);
        setProgress(prev => ({ ...prev, currentOperation: 'Cancelled' }));
        return response;
      }

      const overallSuccess = results.length > 0 && results.every((r) => r.success);
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
      setProgress(prev => ({
        ...prev,
        currentOperation: 'Complete',
        percentComplete: 100,
      }));
      return response;
    } catch (error) {
      const errorMessage = abortRef.current ? 'Migration cancelled by user' : String(error);
      const response: MigrationResponse = {
        success: false,
        error: errorMessage,
      };
      setMigrationResults(response);
      setProgress(prev => ({ ...prev, currentOperation: 'Error' }));
      return response;
    }
  };

  // Dry run to preview what would be migrated
  const runDryRun = async (
    config: MigrationConfig,
    selectedResources: ResourceSelection
  ): Promise<MigrationResponse> => {
    const dryRunConfig: MigrationConfig = { ...config, dryRun: true };
    return runMigration(dryRunConfig, selectedResources, false);
  };

  // Reset state
  const resetMigration = useCallback(() => {
    abortRef.current = false;
    setIsCancelled(false);
    setMigrationResults(null);
    setProgress({
      currentOperation: '',
      completedOperations: [],
      totalOperations: 0,
      percentComplete: 0,
      itemsProcessed: 0,
      itemsTotal: 0,
    });
  }, []);

  return {
    progress,
    migrationResults,
    isCancelled,
    validateConfig,
    runMigration,
    runDryRun,
    cancelMigration,
    resetMigration,
  };
}
