import { useState } from 'react';
import type { WhatIfChange } from '../../../types';
import { getChangeTypeColor, getChangeTypeIcon } from '../utils/whatIfUtils';

interface WhatIfChangeItemProps {
  change: WhatIfChange;
  index: number;
  compact: boolean;
  showSelection: boolean;
  isExpanded: boolean;
  editingResourceGroup: string | null;
  tempResourceGroup: string;
  onToggle: (resourceName: string) => void;
  onToggleSelection: (resourceName: string) => void;
  onStartEditResourceGroup: (resourceName: string, currentGroup: string) => void;
  onUpdateResourceGroup: (resourceName: string, resourceGroup: string) => void;
  onCancelEditResourceGroup: () => void;
  onTempResourceGroupChange: (value: string) => void;
}

function getChangeTypeLabel(changeType: string): string {
  return changeType.charAt(0).toUpperCase() + changeType.slice(1);
}

export function WhatIfChangeItem({
  change,
  index,
  compact,
  showSelection,
  isExpanded,
  editingResourceGroup,
  tempResourceGroup,
  onToggle,
  onToggleSelection,
  onStartEditResourceGroup,
  onUpdateResourceGroup,
  onCancelEditResourceGroup,
  onTempResourceGroupChange,
}: WhatIfChangeItemProps) {
  const hasDetails = change.changes && change.changes.length > 0;
  const isSelected = change.selected !== false;
  const isEditing = editingResourceGroup === change.resourceName;

  return (
    <div
      key={index}
      className={`${getChangeTypeColor(change.changeType)} border-l-4 ${!isSelected && showSelection ? 'opacity-50' : ''}`}
    >
      <div className="flex items-start">
        {showSelection && (
          <div className={`${compact ? 'px-2 py-1.5' : 'px-3 py-3'} flex items-center`}>
            <input
              type="checkbox"
              checked={isSelected}
              onChange={() => onToggleSelection(change.resourceName)}
              className={`${compact ? 'w-3 h-3' : 'w-4 h-4'} text-blue-600 border-gray-300 rounded focus:ring-blue-500`}
              onClick={(e) => e.stopPropagation()}
              aria-label={`Select ${change.resourceName} for deployment`}
            />
          </div>
        )}
        <button
          onClick={() => hasDetails && onToggle(change.resourceName)}
          className={`flex-1 ${compact ? 'px-2 py-1.5' : 'px-4 py-3'} text-left hover:bg-opacity-75 transition-colors`}
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <span className={`font-mono ${compact ? 'text-sm' : 'text-lg'} font-bold`}>
                {getChangeTypeIcon(change.changeType)}
              </span>
              <div className="min-w-0 flex-1">
                <div className={`font-medium ${compact ? 'text-xs truncate' : ''}`} title={change.resourceName}>
                  {change.resourceName}
                </div>
                <div className={`${compact ? 'text-[10px]' : 'text-sm'} opacity-75 truncate`} title={change.resourceType}>
                  {change.resourceType}
                </div>
                {showSelection && !compact && (
                  <div className="text-xs mt-1 flex items-center gap-2">
                    <span className="text-gray-600 dark:text-gray-400">Resource Group:</span>
                    {isEditing ? (
                      <div className="flex items-center gap-1">
                        <input
                          type="text"
                          value={tempResourceGroup}
                          onChange={(e) => onTempResourceGroupChange(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              onUpdateResourceGroup(change.resourceName, tempResourceGroup);
                            }
                            if (e.key === 'Escape') {
                              onCancelEditResourceGroup();
                            }
                          }}
                          className="px-2 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white font-mono"
                          aria-label={`Edit resource group for ${change.resourceName}`}
                          placeholder="Resource group name"
                        />
                        <button
                          onClick={() => onUpdateResourceGroup(change.resourceName, tempResourceGroup)}
                          className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300"
                          title="Save resource group"
                        >
                          ✓
                        </button>
                        <button
                          onClick={onCancelEditResourceGroup}
                          className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
                          title="Cancel editing"
                        >
                          ✕
                        </button>
                      </div>
                    ) : (
                      <span
                        className="ml-2 font-mono cursor-pointer hover:underline"
                        onClick={(e) => {
                          e.stopPropagation();
                          onStartEditResourceGroup(change.resourceName, change.resourceGroup || '');
                        }}
                        title="Click to edit resource group"
                      >
                        {change.resourceGroup || 'Not set'}
                      </span>
                    )}
                  </div>
                )}
              </div>
            </div>
            <div className="flex items-center space-x-2 flex-shrink-0">
              <span className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} font-semibold rounded`}>
                {getChangeTypeLabel(change.changeType)}
              </span>
              {hasDetails && <span className={compact ? 'text-xs' : 'text-sm'}>{isExpanded ? '▼' : '▶'}</span>}
            </div>
          </div>
        </button>
      </div>

      {isExpanded && hasDetails && (
        <div className={compact ? 'px-2 pb-2 pt-1' : 'px-4 pb-3 pt-1'}>
          <div className={`bg-white dark:bg-gray-900 bg-opacity-50 rounded ${compact ? 'p-2 text-[10px]' : 'p-3 text-sm'}`}>
            <div className={`font-medium ${compact ? 'mb-1' : 'mb-2'}`}>Property Changes:</div>
            <ul className="space-y-0.5">
              {change.changes!.map((changeDetail, idx) => (
                <li key={idx} className={`font-mono ${compact ? 'text-[10px] pl-2' : 'text-xs pl-4'} break-all`}>
                  • {changeDetail}
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  );
}

