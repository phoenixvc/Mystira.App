import { useState } from 'react';
import { invoke } from '@tauri-apps/api/tauri';

interface MigrationConfig {
  sourceCosmosConnection: string;
  destCosmosConnection: string;
  sourceStorageConnection: string;
  destStorageConnection: string;
  databaseName: string;
  containerName: string;
}

interface ResourceSelection {
  scenarios: boolean;
  bundles: boolean;
  mediaMetadata: boolean;
  blobStorage: boolean;
}

interface MigrationResult {
  success: boolean;
  totalItems: number;
  successCount: number;
  failureCount: number;
  duration: string;
  errors: string[];
}

interface MigrationResponse {
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

type MigrationStep = 'configure' | 'select' | 'running' | 'complete';

function MigrationManager() {
  const [currentStep, setCurrentStep] = useState<MigrationStep>('configure');
  const [config, setConfig] = useState<MigrationConfig>({
    sourceCosmosConnection: '',
    destCosmosConnection: '',
    sourceStorageConnection: '',
    destStorageConnection: '',
    databaseName: 'MystiraDb',
    containerName: 'media-assets',
  });

  const [selectedResources, setSelectedResources] = useState<ResourceSelection>({
    scenarios: true,
    bundles: true,
    mediaMetadata: true,
    blobStorage: false,
  });

  const [migrationInProgress, setMigrationInProgress] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<string>('');
  const [migrationResults, setMigrationResults] = useState<MigrationResponse | null>(null);

  const handleConfigChange = (field: keyof MigrationConfig, value: string) => {
    setConfig((prev) => ({ ...prev, [field]: value }));
  };

  const handleResourceToggle = (resource: keyof ResourceSelection) => {
    setSelectedResources((prev) => ({ ...prev, [resource]: !prev[resource] }));
  };

  const selectAll = () => {
    setSelectedResources({
      scenarios: true,
      bundles: true,
      mediaMetadata: true,
      blobStorage: true,
    });
  };

  const selectNone = () => {
    setSelectedResources({
      scenarios: false,
      bundles: false,
      mediaMetadata: false,
      blobStorage: false,
    });
  };

  const validateConfig = (): string | null => {
    // Check Cosmos resources
    const needsCosmos = selectedResources.scenarios || selectedResources.bundles || selectedResources.mediaMetadata;
    if (needsCosmos) {
      if (!config.sourceCosmosConnection || !config.destCosmosConnection) {
        return 'Source and destination Cosmos DB connection strings are required for selected resources';
      }
      if (!config.databaseName) {
        return 'Database name is required';
      }
    }

    // Check Blob Storage
    if (selectedResources.blobStorage) {
      if (!config.sourceStorageConnection || !config.destStorageConnection) {
        return 'Source and destination Storage connection strings are required for blob storage migration';
      }
      if (!config.containerName) {
        return 'Container name is required for blob storage migration';
      }
    }

    // Check that at least one resource is selected
    if (!Object.values(selectedResources).some((v) => v)) {
      return 'Please select at least one resource type to migrate';
    }

    return null;
  };

  const startMigration = async () => {
    const validationError = validateConfig();
    if (validationError) {
      alert(validationError);
      return;
    }

    setCurrentStep('running');
    setMigrationInProgress(true);
    setMigrationResults(null);

    try {
      const results: MigrationResult[] = [];
      let totalItems = 0;
      let totalSuccess = 0;
      let totalFailures = 0;

      // Migrate Scenarios
      if (selectedResources.scenarios) {
        setCurrentOperation('Migrating Scenarios...');
        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: 'scenarios',
          sourceCosmos: config.sourceCosmosConnection,
          destCosmos: config.destCosmosConnection,
          sourceStorage: null,
          destStorage: null,
          databaseName: config.databaseName,
          containerName: config.containerName,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }
      }

      // Migrate Content Bundles
      if (selectedResources.bundles) {
        setCurrentOperation('Migrating Content Bundles...');
        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: 'bundles',
          sourceCosmos: config.sourceCosmosConnection,
          destCosmos: config.destCosmosConnection,
          sourceStorage: null,
          destStorage: null,
          databaseName: config.databaseName,
          containerName: config.containerName,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }
      }

      // Migrate Media Assets Metadata
      if (selectedResources.mediaMetadata) {
        setCurrentOperation('Migrating Media Assets Metadata...');
        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: 'media-metadata',
          sourceCosmos: config.sourceCosmosConnection,
          destCosmos: config.destCosmosConnection,
          sourceStorage: null,
          destStorage: null,
          databaseName: config.databaseName,
          containerName: config.containerName,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }
      }

      // Migrate Blob Storage
      if (selectedResources.blobStorage) {
        setCurrentOperation('Migrating Blob Storage Files...');
        const response: MigrationResponse = await invoke('migration_run', {
          migrationType: 'blobs',
          sourceCosmos: null,
          destCosmos: null,
          sourceStorage: config.sourceStorageConnection,
          destStorage: config.destStorageConnection,
          databaseName: config.databaseName,
          containerName: config.containerName,
        });

        if (response.result?.results) {
          results.push(...response.result.results);
          totalItems += response.result.totalItems;
          totalSuccess += response.result.totalSuccess;
          totalFailures += response.result.totalFailures;
        }
      }

      // Set final results
      const overallSuccess = results.every((r) => r.success);
      setMigrationResults({
        success: overallSuccess,
        result: {
          overallSuccess,
          totalItems,
          totalSuccess,
          totalFailures,
          results,
        },
      });

      setCurrentStep('complete');
    } catch (error) {
      setMigrationResults({
        success: false,
        error: String(error),
      });
      setCurrentStep('complete');
    } finally {
      setMigrationInProgress(false);
      setCurrentOperation('');
    }
  };

  const resetMigration = () => {
    setCurrentStep('configure');
    setMigrationResults(null);
    setCurrentOperation('');
  };

  return (
    <div className="p-8">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">Migration Manager</h2>
          <p className="text-gray-600">
            Migrate Cosmos DB data and Azure Blob Storage between environments
          </p>
        </div>

        {/* Step Indicator */}
        <div className="mb-8">
          <div className="flex items-center justify-center space-x-4">
            <div className={`flex items-center ${currentStep === 'configure' ? 'text-blue-600' : 'text-gray-400'}`}>
              <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
                currentStep === 'configure' ? 'border-blue-600 bg-blue-50' : 'border-gray-300 bg-white'
              }`}>
                1
              </div>
              <span className="ml-2 font-medium">Configure</span>
            </div>

            <div className="w-16 h-0.5 bg-gray-300"></div>

            <div className={`flex items-center ${currentStep === 'select' ? 'text-blue-600' : 'text-gray-400'}`}>
              <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
                currentStep === 'select' ? 'border-blue-600 bg-blue-50' : 'border-gray-300 bg-white'
              }`}>
                2
              </div>
              <span className="ml-2 font-medium">Select Resources</span>
            </div>

            <div className="w-16 h-0.5 bg-gray-300"></div>

            <div className={`flex items-center ${currentStep === 'running' || currentStep === 'complete' ? 'text-blue-600' : 'text-gray-400'}`}>
              <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
                currentStep === 'running' || currentStep === 'complete' ? 'border-blue-600 bg-blue-50' : 'border-gray-300 bg-white'
              }`}>
                3
              </div>
              <span className="ml-2 font-medium">Migrate</span>
            </div>
          </div>
        </div>

        {/* Step 1: Configuration */}
        {currentStep === 'configure' && (
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">Connection Configuration</h3>

            {/* Cosmos DB Configuration */}
            <div className="mb-6">
              <h4 className="text-lg font-medium text-gray-900 mb-3">Cosmos DB Connections</h4>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Source Cosmos DB Connection String
                  </label>
                  <input
                    type="text"
                    value={config.sourceCosmosConnection}
                    onChange={(e) => handleConfigChange('sourceCosmosConnection', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="AccountEndpoint=https://source-account.documents.azure.com:443/;AccountKey=..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Destination Cosmos DB Connection String
                  </label>
                  <input
                    type="text"
                    value={config.destCosmosConnection}
                    onChange={(e) => handleConfigChange('destCosmosConnection', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="AccountEndpoint=https://dest-account.documents.azure.com:443/;AccountKey=..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Database Name
                  </label>
                  <input
                    type="text"
                    value={config.databaseName}
                    onChange={(e) => handleConfigChange('databaseName', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="MystiraDb"
                  />
                </div>
              </div>
            </div>

            {/* Blob Storage Configuration */}
            <div className="mb-6">
              <h4 className="text-lg font-medium text-gray-900 mb-3">Blob Storage Connections</h4>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Source Storage Connection String
                  </label>
                  <input
                    type="text"
                    value={config.sourceStorageConnection}
                    onChange={(e) => handleConfigChange('sourceStorageConnection', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="DefaultEndpointsProtocol=https;AccountName=sourcestorage;..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Destination Storage Connection String
                  </label>
                  <input
                    type="text"
                    value={config.destStorageConnection}
                    onChange={(e) => handleConfigChange('destStorageConnection', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="DefaultEndpointsProtocol=https;AccountName=deststorage;..."
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Container Name
                  </label>
                  <input
                    type="text"
                    value={config.containerName}
                    onChange={(e) => handleConfigChange('containerName', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    placeholder="media-assets"
                  />
                </div>
              </div>
            </div>

            <div className="flex justify-end">
              <button
                onClick={() => setCurrentStep('select')}
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                Next: Select Resources
              </button>
            </div>
          </div>
        )}

        {/* Step 2: Resource Selection */}
        {currentStep === 'select' && (
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">Select Resources to Migrate</h3>

            <div className="mb-6">
              <div className="flex gap-3 mb-4">
                <button
                  onClick={selectAll}
                  className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors text-sm"
                >
                  Select All
                </button>
                <button
                  onClick={selectNone}
                  className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors text-sm"
                >
                  Select None
                </button>
              </div>

              <div className="space-y-3">
                <label className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={selectedResources.scenarios}
                    onChange={() => handleResourceToggle('scenarios')}
                    className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
                  />
                  <div className="ml-3">
                    <div className="font-medium text-gray-900">Scenarios</div>
                    <div className="text-sm text-gray-500">
                      Migrate all game scenarios from the Scenarios container
                    </div>
                  </div>
                </label>

                <label className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={selectedResources.bundles}
                    onChange={() => handleResourceToggle('bundles')}
                    className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
                  />
                  <div className="ml-3">
                    <div className="font-medium text-gray-900">Content Bundles</div>
                    <div className="text-sm text-gray-500">
                      Migrate all content bundles from the ContentBundles container
                    </div>
                  </div>
                </label>

                <label className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={selectedResources.mediaMetadata}
                    onChange={() => handleResourceToggle('mediaMetadata')}
                    className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
                  />
                  <div className="ml-3">
                    <div className="font-medium text-gray-900">Media Assets Metadata</div>
                    <div className="text-sm text-gray-500">
                      Migrate media asset records from the MediaAssets container
                    </div>
                  </div>
                </label>

                <label className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={selectedResources.blobStorage}
                    onChange={() => handleResourceToggle('blobStorage')}
                    className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
                  />
                  <div className="ml-3">
                    <div className="font-medium text-gray-900">Blob Storage Files</div>
                    <div className="text-sm text-gray-500">
                      Copy all blob files from source storage container to destination
                    </div>
                  </div>
                </label>
              </div>
            </div>

            <div className="flex justify-between">
              <button
                onClick={() => setCurrentStep('configure')}
                className="px-6 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Back
              </button>
              <button
                onClick={startMigration}
                disabled={!Object.values(selectedResources).some((v) => v)}
                className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Start Migration
              </button>
            </div>
          </div>
        )}

        {/* Step 3: Migration in Progress */}
        {currentStep === 'running' && (
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">Migration in Progress</h3>

            <div className="space-y-6">
              {/* Current Operation */}
              <div className="flex items-center space-x-3">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                <div>
                  <div className="font-medium text-gray-900">{currentOperation}</div>
                  <div className="text-sm text-gray-500">Please wait while migration is in progress...</div>
                </div>
              </div>

              {/* Info */}
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <p className="text-sm text-blue-800">
                  Do not close this window while migration is running. The process may take several minutes depending on the amount of data.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Step 4: Migration Complete */}
        {currentStep === 'complete' && migrationResults && (
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">Migration Complete</h3>

            {/* Overall Result */}
            {migrationResults.success ? (
              <div className="bg-green-50 border border-green-200 rounded-lg p-6 mb-6">
                <div className="flex items-start">
                  <div className="text-4xl mr-4">✅</div>
                  <div className="flex-1">
                    <h4 className="text-lg font-semibold text-green-900 mb-2">
                      Migration Successful
                    </h4>
                    {migrationResults.result && (
                      <div className="text-green-800 space-y-1">
                        <div>
                          <strong>Total Items:</strong> {migrationResults.result.totalItems}
                        </div>
                        <div>
                          <strong>Successful:</strong> {migrationResults.result.totalSuccess}
                        </div>
                        {migrationResults.result.totalFailures > 0 && (
                          <div className="text-yellow-700">
                            <strong>Failed:</strong> {migrationResults.result.totalFailures}
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ) : (
              <div className="bg-red-50 border border-red-200 rounded-lg p-6 mb-6">
                <div className="flex items-start">
                  <div className="text-4xl mr-4">❌</div>
                  <div className="flex-1">
                    <h4 className="text-lg font-semibold text-red-900 mb-2">
                      Migration Failed
                    </h4>
                    <p className="text-red-800">{migrationResults.error || 'An error occurred during migration'}</p>
                  </div>
                </div>
              </div>
            )}

            {/* Detailed Results */}
            {migrationResults.result && migrationResults.result.results.length > 0 && (
              <div className="mb-6">
                <h4 className="font-semibold text-gray-900 mb-3">Detailed Results</h4>
                <div className="space-y-3">
                  {migrationResults.result.results.map((result, index) => (
                    <div
                      key={index}
                      className={`border rounded-lg p-4 ${
                        result.success
                          ? 'border-green-200 bg-green-50'
                          : 'border-red-200 bg-red-50'
                      }`}
                    >
                      <div className="flex justify-between items-start mb-2">
                        <div className="font-medium">
                          {result.success ? '✅' : '❌'} Operation {index + 1}
                        </div>
                        <div className="text-sm text-gray-600">
                          Duration: {result.duration}
                        </div>
                      </div>
                      <div className="text-sm space-y-1">
                        <div>Total Items: {result.totalItems}</div>
                        <div className="text-green-700">Successful: {result.successCount}</div>
                        {result.failureCount > 0 && (
                          <div className="text-red-700">Failed: {result.failureCount}</div>
                        )}
                      </div>

                      {/* Errors */}
                      {result.errors && result.errors.length > 0 && (
                        <div className="mt-3">
                          <div className="font-medium text-red-900 mb-1">Errors:</div>
                          <div className="bg-red-100 rounded p-2 max-h-32 overflow-y-auto">
                            {result.errors.map((error, errorIndex) => (
                              <div key={errorIndex} className="text-sm text-red-800 mb-1">
                                • {error}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex justify-end">
              <button
                onClick={resetMigration}
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                Start New Migration
              </button>
            </div>
          </div>
        )}

        {/* Info Box */}
        <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h4 className="font-semibold text-blue-900 mb-2">ℹ️ Migration Information</h4>
          <ul className="text-sm text-blue-800 space-y-1 list-disc list-inside">
            <li>Migrations use upsert operations (existing items are overwritten)</li>
            <li>Blob storage files are copied only if they don't exist in destination</li>
            <li>Connection strings are not stored and must be re-entered each time</li>
            <li>Large migrations may take several minutes to complete</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

export default MigrationManager;
