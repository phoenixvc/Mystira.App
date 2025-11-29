import type { ResourceGroupConvention, WhatIfChange } from '../../../types';
import WhatIfViewer from '../../WhatIfViewer';

interface WhatIfViewerSectionProps {
  whatIfChanges: WhatIfChange[];
  loading: boolean;
  hasPreviewed: boolean;
  deploymentMethod: 'github' | 'azure-cli';
  resourceGroupConfig: ResourceGroupConvention;
  onWhatIfChangesChange: (changes: WhatIfChange[]) => void;
}

export function WhatIfViewerSection({
  whatIfChanges,
  loading,
  hasPreviewed,
  deploymentMethod,
  resourceGroupConfig,
  onWhatIfChangesChange,
}: WhatIfViewerSectionProps) {
  if (whatIfChanges.length === 0) return null;

  return (
    <div className="mb-8">
      <div className="mb-4 flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
          Resource Changes Preview
        </h3>
        <div className="flex gap-2">
          <button
            onClick={() => {
              const updated = whatIfChanges.map(c => ({ ...c, selected: true }));
              onWhatIfChangesChange(updated);
            }}
            className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
          >
            Select All
          </button>
          <button
            onClick={() => {
              const updated = whatIfChanges.map(c => ({ ...c, selected: false }));
              onWhatIfChangesChange(updated);
            }}
            className="px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded"
          >
            Deselect All
          </button>
        </div>
      </div>
      <WhatIfViewer 
        changes={whatIfChanges} 
        loading={loading}
        showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
        onSelectionChange={onWhatIfChangesChange}
        defaultResourceGroup={resourceGroupConfig.defaultResourceGroup}
        resourceGroupMappings={resourceGroupConfig.resourceTypeMappings || {}}
      />
    </div>
  );
}

