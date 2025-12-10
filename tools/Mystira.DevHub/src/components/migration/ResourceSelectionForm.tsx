import { Database, Users, FileText, HardDrive } from 'lucide-react';
import { ResourceSelection } from './types';

interface ResourceSelectionFormProps {
  selectedResources: ResourceSelection;
  onResourceToggle: (resource: keyof ResourceSelection) => void;
  onSelectAll: () => void;
  onSelectNone: () => void;
  onBack: () => void;
  onStart: () => void;
}

interface ResourceItemProps {
  label: string;
  description: string;
  checked: boolean;
  onChange: () => void;
}

function ResourceItem({ label, description, checked, onChange }: ResourceItemProps) {
  return (
    <label className="flex items-center p-3 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer">
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className="w-4 h-4 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
      />
      <div className="ml-3">
        <div className="font-medium text-gray-900 dark:text-white text-sm">{label}</div>
        <div className="text-xs text-gray-500 dark:text-gray-400">{description}</div>
      </div>
    </label>
  );
}

export function ResourceSelectionForm({
  selectedResources,
  onResourceToggle,
  onSelectAll,
  onSelectNone,
  onBack,
  onStart,
}: ResourceSelectionFormProps) {
  const selectedCount = Object.values(selectedResources).filter(Boolean).length;
  const totalCount = Object.keys(selectedResources).length;

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white">Select Resources to Migrate</h3>
        <span className="text-sm text-gray-500 dark:text-gray-400">
          {selectedCount} of {totalCount} selected
        </span>
      </div>

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

        {/* Core Content Section */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-3">
            <Database className="w-4 h-4 text-blue-500" />
            <h4 className="font-medium text-gray-900 dark:text-white">Core Content</h4>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            <ResourceItem
              label="Scenarios"
              description="Game scenarios and story content"
              checked={selectedResources.scenarios}
              onChange={() => onResourceToggle('scenarios')}
            />
            <ResourceItem
              label="Content Bundles"
              description="Packaged content bundles"
              checked={selectedResources.bundles}
              onChange={() => onResourceToggle('bundles')}
            />
            <ResourceItem
              label="Media Assets"
              description="Media asset metadata records"
              checked={selectedResources.mediaMetadata}
              onChange={() => onResourceToggle('mediaMetadata')}
            />
          </div>
        </div>

        {/* User Data Section */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-3">
            <Users className="w-4 h-4 text-green-500" />
            <h4 className="font-medium text-gray-900 dark:text-white">User Data</h4>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            <ResourceItem
              label="User Profiles"
              description="User profile information"
              checked={selectedResources.userProfiles}
              onChange={() => onResourceToggle('userProfiles')}
            />
            <ResourceItem
              label="Game Sessions"
              description="Player game session data"
              checked={selectedResources.gameSessions}
              onChange={() => onResourceToggle('gameSessions')}
            />
            <ResourceItem
              label="Accounts"
              description="User account records"
              checked={selectedResources.accounts}
              onChange={() => onResourceToggle('accounts')}
            />
            <ResourceItem
              label="Compass Trackings"
              description="User compass tracking data"
              checked={selectedResources.compassTrackings}
              onChange={() => onResourceToggle('compassTrackings')}
            />
          </div>
        </div>

        {/* Reference Data Section */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-3">
            <FileText className="w-4 h-4 text-purple-500" />
            <h4 className="font-medium text-gray-900 dark:text-white">Reference Data</h4>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            <ResourceItem
              label="Character Maps"
              description="Character mapping definitions"
              checked={selectedResources.characterMaps}
              onChange={() => onResourceToggle('characterMaps')}
            />
            <ResourceItem
              label="Character Map Files"
              description="Character map file data"
              checked={selectedResources.characterMapFiles}
              onChange={() => onResourceToggle('characterMapFiles')}
            />
            <ResourceItem
              label="Character Media Files"
              description="Character media metadata files"
              checked={selectedResources.characterMediaMetadataFiles}
              onChange={() => onResourceToggle('characterMediaMetadataFiles')}
            />
            <ResourceItem
              label="Avatar Configurations"
              description="Avatar configuration files"
              checked={selectedResources.avatarConfigurationFiles}
              onChange={() => onResourceToggle('avatarConfigurationFiles')}
            />
            <ResourceItem
              label="Badge Configurations"
              description="Badge configuration data"
              checked={selectedResources.badgeConfigurations}
              onChange={() => onResourceToggle('badgeConfigurations')}
            />
          </div>
        </div>

        {/* Storage Section */}
        <div className="mb-6">
          <div className="flex items-center gap-2 mb-3">
            <HardDrive className="w-4 h-4 text-orange-500" />
            <h4 className="font-medium text-gray-900 dark:text-white">Blob Storage</h4>
          </div>
          <div className="grid grid-cols-1 gap-2">
            <ResourceItem
              label="Blob Storage Files"
              description="Copy all blob files from source storage container to destination"
              checked={selectedResources.blobStorage}
              onChange={() => onResourceToggle('blobStorage')}
            />
          </div>
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
          disabled={selectedCount === 0}
          className="px-6 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Start Migration ({selectedCount} resources)
        </button>
      </div>
    </div>
  );
}
