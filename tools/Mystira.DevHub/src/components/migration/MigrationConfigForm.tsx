import { MigrationConfig } from './types';

interface MigrationConfigFormProps {
  config: MigrationConfig;
  onConfigChange: (field: keyof MigrationConfig, value: string) => void;
  onNext: () => void;
}

export function MigrationConfigForm({ config, onConfigChange, onNext }: MigrationConfigFormProps) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Connection Configuration</h3>

      <div className="mb-6">
        <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-3">Cosmos DB Connections</h4>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Source Cosmos DB Connection String
            </label>
            <input
              type="text"
              value={config.sourceCosmosConnection}
              onChange={(e) => onConfigChange('sourceCosmosConnection', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="AccountEndpoint=https://source-account.documents.azure.com:443/;AccountKey=..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Destination Cosmos DB Connection String
            </label>
            <input
              type="text"
              value={config.destCosmosConnection}
              onChange={(e) => onConfigChange('destCosmosConnection', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="AccountEndpoint=https://dest-account.documents.azure.com:443/;AccountKey=..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Database Name
            </label>
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

      <div className="mb-6">
        <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-3">Blob Storage Connections</h4>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Source Storage Connection String
            </label>
            <input
              type="text"
              value={config.sourceStorageConnection}
              onChange={(e) => onConfigChange('sourceStorageConnection', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="DefaultEndpointsProtocol=https;AccountName=sourcestorage;..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Destination Storage Connection String
            </label>
            <input
              type="text"
              value={config.destStorageConnection}
              onChange={(e) => onConfigChange('destStorageConnection', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="DefaultEndpointsProtocol=https;AccountName=deststorage;..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Container Name
            </label>
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

      <div className="flex justify-end">
        <button
          onClick={onNext}
          className="px-6 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors"
        >
          Next: Select Resources
        </button>
      </div>
    </div>
  );
}

