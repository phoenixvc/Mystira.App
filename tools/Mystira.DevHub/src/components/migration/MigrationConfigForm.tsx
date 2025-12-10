import { useState } from 'react';
import { ChevronDown, ChevronUp } from 'lucide-react';
import { EnvironmentSelector } from './EnvironmentSelector';
import { MigrationConfig } from './types';

interface MigrationConfigFormProps {
  config: MigrationConfig;
  onConfigChange: (field: keyof MigrationConfig, value: string) => void;
  onNext: () => void;
}

export function MigrationConfigForm({ config, onConfigChange, onNext }: MigrationConfigFormProps) {
  const [showManualEntry, setShowManualEntry] = useState(false);
  const isCustomMode = config.sourceEnvironment === 'custom' || config.destEnvironment === 'custom';

  const handleConnectionsFetched = (
    source: { cosmos: string; storage: string },
    dest: { cosmos: string; storage: string }
  ) => {
    onConfigChange('sourceCosmosConnection', source.cosmos);
    onConfigChange('sourceStorageConnection', source.storage);
    onConfigChange('destCosmosConnection', dest.cosmos);
    onConfigChange('destStorageConnection', dest.storage);
  };

  const hasConnections =
    config.sourceCosmosConnection || config.destCosmosConnection || config.sourceStorageConnection || config.destStorageConnection;

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Connection Configuration</h3>

      {/* Environment Selector */}
      <EnvironmentSelector
        sourceEnvironment={config.sourceEnvironment}
        destEnvironment={config.destEnvironment}
        onSourceChange={(id) => onConfigChange('sourceEnvironment', id)}
        onDestChange={(id) => onConfigChange('destEnvironment', id)}
        onConnectionsFetched={handleConnectionsFetched}
      />

      {/* Manual Entry Toggle */}
      <div className="border-t border-gray-200 dark:border-gray-700 pt-4 mt-4">
        <button
          onClick={() => setShowManualEntry(!showManualEntry)}
          className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white transition-colors"
        >
          {showManualEntry ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
          {isCustomMode ? 'Connection String Entry (Required)' : 'Manual Connection String Entry'}
          {hasConnections && !isCustomMode && (
            <span className="text-green-600 dark:text-green-400 text-xs ml-2">(Auto-filled)</span>
          )}
        </button>
      </div>

      {/* Manual Entry Fields - Always show for custom mode, toggleable otherwise */}
      {(showManualEntry || isCustomMode) && (
        <div className="mt-4 space-y-6">
          <div>
            <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-3">Cosmos DB Connections</h4>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Source Cosmos DB Connection String
                </label>
                <input
                  type="password"
                  value={config.sourceCosmosConnection}
                  onChange={(e) => onConfigChange('sourceCosmosConnection', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
                  placeholder="AccountEndpoint=https://source-account.documents.azure.com:443/;AccountKey=..."
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Destination Cosmos DB Connection String
                </label>
                <input
                  type="password"
                  value={config.destCosmosConnection}
                  onChange={(e) => onConfigChange('destCosmosConnection', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
                  placeholder="AccountEndpoint=https://dest-account.documents.azure.com:443/;AccountKey=..."
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Database Name</label>
                <input
                  type="text"
                  value={config.databaseName}
                  onChange={(e) => onConfigChange('databaseName', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="MystiraDb"
                />
              </div>
            </div>
          </div>

          <div>
            <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-3">Blob Storage Connections</h4>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Source Storage Connection String
                </label>
                <input
                  type="password"
                  value={config.sourceStorageConnection}
                  onChange={(e) => onConfigChange('sourceStorageConnection', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
                  placeholder="DefaultEndpointsProtocol=https;AccountName=sourcestorage;..."
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Destination Storage Connection String
                </label>
                <input
                  type="password"
                  value={config.destStorageConnection}
                  onChange={(e) => onConfigChange('destStorageConnection', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent font-mono text-sm"
                  placeholder="DefaultEndpointsProtocol=https;AccountName=deststorage;..."
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Container Name</label>
                <input
                  type="text"
                  value={config.containerName}
                  onChange={(e) => onConfigChange('containerName', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="media-assets"
                />
              </div>
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-end mt-6">
        <button
          onClick={onNext}
          disabled={!config.sourceEnvironment || !config.destEnvironment}
          className="px-6 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Next: Select Resources
        </button>
      </div>
    </div>
  );
}
