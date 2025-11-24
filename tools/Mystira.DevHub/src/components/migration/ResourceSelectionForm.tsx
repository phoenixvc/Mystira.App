import { ResourceSelection } from './types';

interface ResourceSelectionFormProps {
  selectedResources: ResourceSelection;
  onResourceToggle: (resource: keyof ResourceSelection) => void;
  onSelectAll: () => void;
  onSelectNone: () => void;
  onBack: () => void;
  onStart: () => void;
}

export function ResourceSelectionForm({
  selectedResources,
  onResourceToggle,
  onSelectAll,
  onSelectNone,
  onBack,
  onStart,
}: ResourceSelectionFormProps) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Select Resources to Migrate</h3>

      <div className="mb-6">
        <div className="flex gap-3 mb-4">
          <button
            onClick={onSelectAll}
            className="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors text-sm"
          >
            Select All
          </button>
          <button
            onClick={onSelectNone}
            className="px-4 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors text-sm"
          >
            Select None
          </button>
        </div>

        <div className="space-y-3">
          <label className="flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedResources.scenarios}
              onChange={() => onResourceToggle('scenarios')}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div className="ml-3">
              <div className="font-medium text-gray-900 dark:text-white">Scenarios</div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Migrate all game scenarios from the Scenarios container
              </div>
            </div>
          </label>

          <label className="flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedResources.bundles}
              onChange={() => onResourceToggle('bundles')}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div className="ml-3">
              <div className="font-medium text-gray-900 dark:text-white">Content Bundles</div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Migrate all content bundles from the ContentBundles container
              </div>
            </div>
          </label>

          <label className="flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedResources.mediaMetadata}
              onChange={() => onResourceToggle('mediaMetadata')}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div className="ml-3">
              <div className="font-medium text-gray-900 dark:text-white">Media Assets Metadata</div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Migrate media asset records from the MediaAssets container
              </div>
            </div>
          </label>

          <label className="flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedResources.blobStorage}
              onChange={() => onResourceToggle('blobStorage')}
              className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
            />
            <div className="ml-3">
              <div className="font-medium text-gray-900 dark:text-white">Blob Storage Files</div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Copy all blob files from source storage container to destination
              </div>
            </div>
          </label>
        </div>
      </div>

      <div className="flex justify-between">
        <button
          onClick={onBack}
          className="px-6 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
        >
          Back
        </button>
        <button
          onClick={onStart}
          disabled={!Object.values(selectedResources).some((v) => v)}
          className="px-6 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Start Migration
        </button>
      </div>
    </div>
  );
}

