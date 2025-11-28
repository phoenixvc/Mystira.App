import { useEffect, useState } from 'react';
import type { WhatIfChange } from '../types';
import { WhatIfSummary, WhatIfChangeItem, WhatIfWarnings } from './what-if/components';

interface WhatIfViewerProps {
  changes?: WhatIfChange[];
  loading?: boolean;
  onSelectionChange?: (changes: WhatIfChange[]) => void;
  showSelection?: boolean;
  compact?: boolean;
  defaultResourceGroup?: string;
  resourceGroupMappings?: Record<string, string>;
}

function WhatIfViewer({
  changes,
  loading,
  onSelectionChange,
  showSelection = false,
  compact = false,
  defaultResourceGroup = 'dev-euw-rg-mystira-app',
  resourceGroupMappings = {},
}: WhatIfViewerProps) {
  const [expandedResources, setExpandedResources] = useState<Set<string>>(new Set());
  const [localChanges, setLocalChanges] = useState<WhatIfChange[]>(changes || []);
  const [editingResourceGroup, setEditingResourceGroup] = useState<string | null>(null);
  const [tempResourceGroup, setTempResourceGroup] = useState<string>('');

  useEffect(() => {
    if (changes) {
      const withSelection = changes.map((c) => {
        const resourceGroup =
          c.resourceGroup || resourceGroupMappings[c.resourceType] || defaultResourceGroup;
        return { ...c, selected: c.selected !== false, resourceGroup };
      });
      setLocalChanges(withSelection);
    }
  }, [changes, defaultResourceGroup, resourceGroupMappings]);

  const toggleResourceSelection = (resourceName: string) => {
    const updated = localChanges.map((change) =>
      change.resourceName === resourceName ? { ...change, selected: !change.selected } : change
    );
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };

  const updateResourceGroup = (resourceName: string, resourceGroup: string) => {
    const updated = localChanges.map((change) =>
      change.resourceName === resourceName ? { ...change, resourceGroup } : change
    );
    setLocalChanges(updated);
    setEditingResourceGroup(null);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };

  const selectAll = () => {
    const updated = localChanges.map((change) => ({ ...change, selected: true }));
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };

  const deselectAll = () => {
    const updated = localChanges.map((change) => ({ ...change, selected: false }));
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };

  const toggleResource = (resourceName: string) => {
    setExpandedResources((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(resourceName)) {
        newSet.delete(resourceName);
      } else {
        newSet.add(resourceName);
      }
      return newSet;
    });
  };

  const selectedCount = localChanges.filter((c) => c.selected).length;
  const createCount = localChanges.filter((c) => c.changeType === 'create').length;
  const modifyCount = localChanges.filter((c) => c.changeType === 'modify').length;
  const deleteCount = localChanges.filter((c) => c.changeType === 'delete').length;
  const noChangeCount = localChanges.filter((c) => c.changeType === 'noChange').length;

  if (loading) {
    return (
      <div className={`border border-gray-200 dark:border-gray-700 rounded-lg ${compact ? 'p-4' : 'p-8'}`}>
        <div className="flex items-center justify-center">
          <div
            className={`animate-spin rounded-full ${compact ? 'h-6 w-6' : 'h-12 w-12'} border-b-2 border-blue-600 dark:border-blue-400 mr-3`}
          ></div>
          <div className={`text-gray-700 dark:text-gray-300 ${compact ? 'text-xs' : ''}`}>
            Analyzing infrastructure changes...
          </div>
        </div>
      </div>
    );
  }

  if (!localChanges || localChanges.length === 0) {
    return (
      <div className={`border border-gray-200 dark:border-gray-700 rounded-lg ${compact ? 'p-4' : 'p-8'} text-center`}>
        <div className={`${compact ? 'text-2xl' : 'text-4xl'} mb-3`}>📋</div>
        <div className={`text-gray-700 dark:text-gray-300 font-medium ${compact ? 'text-xs' : 'mb-2'}`}>
          No Changes Detected
        </div>
        <div className={`${compact ? 'text-[10px]' : 'text-sm'} text-gray-500 dark:text-gray-400`}>
          Infrastructure is up to date with the template
        </div>
      </div>
    );
  }

  return (
    <div className={`border border-gray-200 dark:border-gray-700 rounded-lg ${compact ? 'p-3' : 'p-6'} bg-white dark:bg-gray-800`}>
      {showSelection && (
        <div className={`flex items-center justify-between ${compact ? 'mb-2' : 'mb-4'}`}>
          <div className={`${compact ? 'text-xs' : 'text-sm'} text-gray-600 dark:text-gray-400`}>
            {selectedCount} of {localChanges.length} selected
          </div>
          <div className="flex gap-2">
            <button
              onClick={selectAll}
              className={`${compact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-1.5'} bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors`}
            >
              Select All
            </button>
            <button
              onClick={deselectAll}
              className={`${compact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-1.5'} bg-gray-600 text-white rounded hover:bg-gray-700 transition-colors`}
            >
              Deselect All
            </button>
          </div>
        </div>
      )}

      <WhatIfSummary
        createCount={createCount}
        modifyCount={modifyCount}
        deleteCount={deleteCount}
        noChangeCount={noChangeCount}
        compact={compact}
      />

      <div className={`divide-y divide-gray-200 dark:divide-gray-700 ${compact ? 'max-h-48' : 'max-h-96'} overflow-y-auto`}>
        {localChanges.map((change, index) => (
          <WhatIfChangeItem
            key={index}
            change={change}
            index={index}
            compact={compact}
            showSelection={showSelection}
            isExpanded={expandedResources.has(change.resourceName)}
            editingResourceGroup={editingResourceGroup}
            tempResourceGroup={tempResourceGroup}
            onToggle={toggleResource}
            onToggleSelection={toggleResourceSelection}
            onStartEditResourceGroup={(name, current) => {
              setEditingResourceGroup(name);
              setTempResourceGroup(current);
            }}
            onUpdateResourceGroup={updateResourceGroup}
            onCancelEditResourceGroup={() => setEditingResourceGroup(null)}
            onTempResourceGroupChange={setTempResourceGroup}
          />
        ))}
      </div>

      <WhatIfWarnings
        deleteCount={deleteCount}
        showSelection={showSelection}
        localChanges={localChanges}
        compact={compact}
      />
    </div>
  );
}

export default WhatIfViewer;

