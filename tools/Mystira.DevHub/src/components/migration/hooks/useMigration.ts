import { invoke } from '@tauri-apps/api/tauri';
import { useState } from 'react';
import { MigrationConfig, MigrationResponse, MigrationResult, ResourceSelection } from '../types';

export function useMigration() {
  const [currentOperation, setCurrentOperation] = useState<string>('');
  const [migrationResults, setMigrationResults] = useState<MigrationResponse | null>(null);

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
      if (!config.sourceDatabaseName || !config.destDatabaseName) {
        return 'Source and destination database names are required';
      }
    }

    if (needsDestCosmos && !needsCosmos) {
      if (!config.destCosmosConnection) {
        return 'Destination Cosmos DB connection string is required for master data seeding';
      }
      if (!config.destDatabaseName) {
        return 'Destination database name is required for master data seeding';
      }
    }

    if (selectedResources.blobStorage) {
      if (!config.sourceStorageConnection || !config.destStorageConnection) {
        return 'Source and destination Storage connection strings are required for blob storage migration';
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

  const runMigration = async (
    config: MigrationConfig,
    selectedResources: ResourceSelection
  ): Promise<MigrationResponse> => {
    setCurrentOperation('');
    setMigrationResults(null);

    try {
      const results: MigrationResult[] = [];
      let totalItems = 0;
      let totalSuccess = 0;
      let totalFailures = 0;

      const migrateResource = async (type: string, operationName: string) => {
        setCurrentOperation(operationName);
        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: type,
          sourceCosmos: config.sourceCosmosConnection || null,
          destCosmos: config.destCosmosConnection || null,
          sourceStorage: config.sourceStorageConnection || null,
          destStorage: config.destStorageConnection || null,
          sourceDatabaseName: config.sourceDatabaseName,
          destDatabaseName: config.destDatabaseName,
          containerName: config.containerName,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }
      };

      // Core content
      if (selectedResources.scenarios) {
        await migrateResource('scenarios', 'Migrating Scenarios...');
      }

      if (selectedResources.bundles) {
        await migrateResource('bundles', 'Migrating Content Bundles...');
      }

      if (selectedResources.mediaMetadata) {
        await migrateResource('media-metadata', 'Migrating Media Assets Metadata...');
      }

      // User data
      if (selectedResources.userProfiles) {
        await migrateResource('user-profiles', 'Migrating User Profiles...');
      }

      if (selectedResources.gameSessions) {
        await migrateResource('game-sessions', 'Migrating Game Sessions...');
      }

      if (selectedResources.accounts) {
        await migrateResource('accounts', 'Migrating Accounts...');
      }

      if (selectedResources.compassTrackings) {
        await migrateResource('compass-trackings', 'Migrating Compass Trackings...');
      }

      // Reference data
      if (selectedResources.characterMaps) {
        await migrateResource('character-maps', 'Migrating Character Maps...');
      }

      if (selectedResources.characterMapFiles) {
        await migrateResource('character-map-files', 'Migrating Character Map Files...');
      }

      if (selectedResources.characterMediaMetadataFiles) {
        await migrateResource('character-media-metadata-files', 'Migrating Character Media Files...');
      }

      if (selectedResources.avatarConfigurationFiles) {
        await migrateResource('avatar-configuration-files', 'Migrating Avatar Configurations...');
      }

      if (selectedResources.badgeConfigurations) {
        await migrateResource('badge-configurations', 'Migrating Badge Configurations...');
      }

      // Master data seeding
      if (selectedResources.masterData) {
        await migrateResource('master-data', 'Seeding Master Data...');
      }

      // Blob storage
      if (selectedResources.blobStorage) {
        await migrateResource('blobs', 'Migrating Blob Storage Files...');
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
      return response;
    } catch (error) {
      const response: MigrationResponse = {
        success: false,
        error: String(error),
      };
      setMigrationResults(response);
      setCurrentOperation('');
      return response;
    }
  };

  return {
    currentOperation,
    migrationResults,
    validateConfig,
    runMigration,
  };
}
