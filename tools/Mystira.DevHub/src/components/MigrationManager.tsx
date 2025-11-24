import { useState } from 'react';
import { MigrationConfigForm } from './migration/MigrationConfigForm';
import { MigrationProgress } from './migration/MigrationProgress';
import { MigrationResults } from './migration/MigrationResults';
import { MigrationStepIndicator } from './migration/MigrationStepIndicator';
import { ResourceSelectionForm } from './migration/ResourceSelectionForm';
import { useMigration } from './migration/hooks/useMigration';
import { MigrationConfig, MigrationStep, ResourceSelection } from './migration/types';

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

  const { currentOperation, migrationResults, validateConfig, runMigration } = useMigration();

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

  const startMigration = async () => {
    const validationError = validateConfig(config, selectedResources);
    if (validationError) {
      alert(validationError);
      return;
    }

    setCurrentStep('running');
    await runMigration(config, selectedResources);
    setCurrentStep('complete');
  };

  const resetMigration = () => {
    setCurrentStep('configure');
    setConfig({
      sourceCosmosConnection: '',
      destCosmosConnection: '',
      sourceStorageConnection: '',
      destStorageConnection: '',
      databaseName: 'MystiraDb',
      containerName: 'media-assets',
    });
    setSelectedResources({
      scenarios: true,
      bundles: true,
      mediaMetadata: true,
      blobStorage: false,
    });
  };

  return (
    <div className="p-8">
      <div className="max-w-6xl mx-auto">
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">Migration Manager</h2>
          <p className="text-gray-600 dark:text-gray-400">
            Migrate Cosmos DB data and Azure Blob Storage between environments
          </p>
        </div>

        <MigrationStepIndicator currentStep={currentStep} />

        {currentStep === 'configure' && (
          <MigrationConfigForm
            config={config}
            onConfigChange={handleConfigChange}
            onNext={() => setCurrentStep('select')}
          />
        )}

        {currentStep === 'select' && (
          <ResourceSelectionForm
            selectedResources={selectedResources}
            onResourceToggle={handleResourceToggle}
            onSelectAll={selectAll}
            onSelectNone={selectNone}
            onBack={() => setCurrentStep('configure')}
            onStart={startMigration}
          />
        )}

        {currentStep === 'running' && (
          <MigrationProgress currentOperation={currentOperation} />
        )}

        {currentStep === 'complete' && migrationResults && (
          <MigrationResults results={migrationResults} onReset={resetMigration} />
        )}
      </div>
    </div>
  );
}

export default MigrationManager;

