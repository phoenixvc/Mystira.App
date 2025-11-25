import { useState, useEffect } from 'react';

interface WhatIfChange {
  resourceType: string;
  resourceName: string;
  changeType: 'create' | 'modify' | 'delete' | 'noChange';
  changes?: string[];
  selected?: boolean;
  resourceId?: string;
}

interface WhatIfViewerProps {
  changes?: WhatIfChange[];
  loading?: boolean;
  onSelectionChange?: (changes: WhatIfChange[]) => void;
  showSelection?: boolean;
}

function WhatIfViewer({ changes, loading, onSelectionChange, showSelection = false }: WhatIfViewerProps) {
  const [expandedResources, setExpandedResources] = useState<Set<string>>(new Set());
  const [localChanges, setLocalChanges] = useState<WhatIfChange[]>(changes || []);
  
  // Sync local changes with props
  useState(() => {
    if (changes) {
      const withSelection = changes.map(c => ({ ...c, selected: c.selected !== false }));
      setLocalChanges(withSelection);
    }
  });
  
  const toggleResourceSelection = (resourceName: string) => {
    const updated = localChanges.map(change => 
      change.resourceName === resourceName 
        ? { ...change, selected: !change.selected }
        : change
    );
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };
  
  const selectAll = () => {
    const updated = localChanges.map(change => ({ ...change, selected: true }));
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };
  
  const deselectAll = () => {
    const updated = localChanges.map(change => ({ ...change, selected: false }));
    setLocalChanges(updated);
    if (onSelectionChange) {
      onSelectionChange(updated);
    }
  };
  
  const selectedCount = localChanges.filter(c => c.selected).length;

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

  const getChangeTypeColor = (changeType: string) => {
    switch (changeType) {
      case 'create':
        return 'text-green-700 bg-green-50 border-green-200';
      case 'modify':
        return 'text-yellow-700 bg-yellow-50 border-yellow-200';
      case 'delete':
        return 'text-red-700 bg-red-50 border-red-200';
      case 'noChange':
        return 'text-gray-700 bg-gray-50 border-gray-200';
      default:
        return 'text-gray-700 bg-gray-50 border-gray-200';
    }
  };

  const getChangeTypeIcon = (changeType: string) => {
    switch (changeType) {
      case 'create':
        return '+ ';
      case 'modify':
        return '~ ';
      case 'delete':
        return '- ';
      case 'noChange':
        return '= ';
      default:
        return '? ';
    }
  };

  const getChangeTypeLabel = (changeType: string) => {
    switch (changeType) {
      case 'create':
        return 'Create';
      case 'modify':
        return 'Modify';
      case 'delete':
        return 'Delete';
      case 'noChange':
        return 'No Change';
      default:
        return 'Unknown';
    }
  };

  if (loading) {
    return (
      <div className="border border-gray-200 rounded-lg p-8">
        <div className="flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mr-4"></div>
          <div className="text-gray-700">Analyzing infrastructure changes...</div>
        </div>
      </div>
    );
  }

  if (!changes || changes.length === 0) {
    return (
      <div className="border border-gray-200 rounded-lg p-8 text-center">
        <div className="text-4xl mb-3">🔍</div>
        <div className="text-gray-700 font-medium mb-2">No Changes Preview Available</div>
        <div className="text-gray-500 text-sm">
          Run "Preview Changes" to see what will be deployed
        </div>
      </div>
    );
  }

  const createCount = changes.filter((c) => c.changeType === 'create').length;
  const modifyCount = changes.filter((c) => c.changeType === 'modify').length;
  const deleteCount = changes.filter((c) => c.changeType === 'delete').length;
  const noChangeCount = changes.filter((c) => c.changeType === 'noChange').length;

  return (
    <div className="border border-gray-200 rounded-lg overflow-hidden">
      {/* Summary Header */}
      <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-gray-200 p-4">
        <div className="flex items-center justify-between mb-3">
          <h3 className="font-semibold text-gray-900">Infrastructure Changes Preview</h3>
          {showSelection && (
            <div className="flex items-center gap-2">
              <button
                onClick={selectAll}
                className="text-xs px-2 py-1 bg-white border border-gray-300 rounded hover:bg-gray-50"
              >
                Select All
              </button>
              <button
                onClick={deselectAll}
                className="text-xs px-2 py-1 bg-white border border-gray-300 rounded hover:bg-gray-50"
              >
                Deselect All
              </button>
              <span className="text-xs text-gray-600">
                {selectedCount} of {localChanges.length} selected
              </span>
            </div>
          )}
        </div>
        <div className="grid grid-cols-4 gap-3">
          {createCount > 0 && (
            <div className="bg-white border border-green-200 rounded-lg p-3 text-center">
              <div className="text-2xl font-bold text-green-700">{createCount}</div>
              <div className="text-xs text-green-600 font-medium mt-1">To Create</div>
            </div>
          )}
          {modifyCount > 0 && (
            <div className="bg-white border border-yellow-200 rounded-lg p-3 text-center">
              <div className="text-2xl font-bold text-yellow-700">{modifyCount}</div>
              <div className="text-xs text-yellow-600 font-medium mt-1">To Modify</div>
            </div>
          )}
          {deleteCount > 0 && (
            <div className="bg-white border border-red-200 rounded-lg p-3 text-center">
              <div className="text-2xl font-bold text-red-700">{deleteCount}</div>
              <div className="text-xs text-red-600 font-medium mt-1">To Delete</div>
            </div>
          )}
          {noChangeCount > 0 && (
            <div className="bg-white border border-gray-200 rounded-lg p-3 text-center">
              <div className="text-2xl font-bold text-gray-700">{noChangeCount}</div>
              <div className="text-xs text-gray-600 font-medium mt-1">No Change</div>
            </div>
          )}
        </div>
      </div>

      {/* Changes List */}
      <div className="divide-y divide-gray-200 max-h-96 overflow-y-auto">
        {localChanges.map((change, index) => {
          const isExpanded = expandedResources.has(change.resourceName);
          const hasDetails = change.changes && change.changes.length > 0;
          const isSelected = change.selected !== false;

          return (
            <div
              key={index}
              className={`${getChangeTypeColor(change.changeType)} border-l-4 ${!isSelected && showSelection ? 'opacity-50' : ''}`}
            >
              <div className="flex items-start">
                {showSelection && (
                  <div className="px-3 py-3 flex items-center">
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => toggleResourceSelection(change.resourceName)}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                      onClick={(e) => e.stopPropagation()}
                    />
                  </div>
                )}
                <button
                  onClick={() => hasDetails && toggleResource(change.resourceName)}
                  className="flex-1 px-4 py-3 text-left hover:bg-opacity-75 transition-colors"
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <span className="font-mono text-lg font-bold">
                        {getChangeTypeIcon(change.changeType)}
                      </span>
                      <div>
                        <div className="font-medium">{change.resourceName}</div>
                        <div className="text-sm opacity-75">{change.resourceType}</div>
                      </div>
                    </div>
                    <div className="flex items-center space-x-2">
                      <span className="text-xs font-semibold px-2 py-1 rounded">
                        {getChangeTypeLabel(change.changeType)}
                      </span>
                      {hasDetails && (
                        <span className="text-sm">
                          {isExpanded ? '▼' : '▶'}
                        </span>
                      )}
                    </div>
                  </div>
                </button>
              </div>

              {/* Expanded Details */}
              {isExpanded && hasDetails && (
                <div className="px-4 pb-3 pt-1">
                  <div className="bg-white bg-opacity-50 rounded p-3 text-sm">
                    <div className="font-medium mb-2">Property Changes:</div>
                    <ul className="space-y-1">
                      {change.changes!.map((changeDetail, idx) => (
                        <li key={idx} className="font-mono text-xs pl-4">
                          • {changeDetail}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Warning for destructive changes */}
      {deleteCount > 0 && (
        <div className="bg-red-50 border-t border-red-200 p-4">
          <div className="flex items-start">
            <span className="text-2xl mr-3">⚠️</span>
            <div>
              <div className="font-semibold text-red-900 mb-1">Warning: Destructive Changes</div>
              <div className="text-sm text-red-800">
                {deleteCount} resource{deleteCount > 1 ? 's' : ''} will be deleted. This action cannot be undone.
                Please review carefully before deploying.
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Dependency warnings */}
      {showSelection && (() => {
        const selectedModules = new Set(
          localChanges
            .filter(c => c.selected !== false)
            .map(c => {
              const type = (c.resourceType || '').toLowerCase();
              if (type.includes('web/sites') || type.includes('appservice') || type.includes('web/serverfarms')) return 'appservice';
              if (type.includes('documentdb') || type.includes('cosmos')) return 'cosmos';
              if (type.includes('storage') && type.includes('account')) return 'storage';
              return null;
            })
            .filter(Boolean)
        );
        
        const hasAppService = selectedModules.has('appservice');
        const hasCosmos = selectedModules.has('cosmos');
        const hasStorage = selectedModules.has('storage');
        const missingDeps: string[] = [];
        
        if (hasAppService) {
          if (!hasCosmos) missingDeps.push('Cosmos DB');
          if (!hasStorage) missingDeps.push('Storage Account');
        }
        
        return missingDeps.length > 0 ? (
          <div className="bg-yellow-50 border-t border-yellow-200 p-4">
            <div className="flex items-start">
              <span className="text-2xl mr-3">⚠️</span>
              <div>
                <div className="font-semibold text-yellow-900 mb-1">Dependency Warning</div>
                <div className="text-sm text-yellow-800">
                  App Service is selected but {missingDeps.join(' and ')} {missingDeps.length === 1 ? 'is' : 'are'} not selected.
                  App Service requires these dependencies to function. Please select {missingDeps.join(' and ')} before deploying.
                </div>
              </div>
            </div>
          </div>
        ) : null;
      })()}
    </div>
  );
}

export default WhatIfViewer;
