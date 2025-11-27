import { useState } from 'react';

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
  compact?: boolean;
}

function WhatIfViewer({ changes, loading, onSelectionChange, showSelection = false, compact = false }: WhatIfViewerProps) {
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
      <div className={`border border-gray-200 dark:border-gray-700 rounded-lg ${compact ? 'p-4' : 'p-8'}`}>
        <div className="flex items-center justify-center">
          <div className={`animate-spin rounded-full ${compact ? 'h-6 w-6' : 'h-12 w-12'} border-b-2 border-blue-600 dark:border-blue-400 mr-3`}></div>
          <div className={`text-gray-700 dark:text-gray-300 ${compact ? 'text-xs' : ''}`}>Analyzing infrastructure changes...</div>
        </div>
      </div>
    );
  }

  if (!changes || changes.length === 0) {
    return (
      <div className={`border border-gray-200 dark:border-gray-700 rounded-lg ${compact ? 'p-4' : 'p-8'} text-center`}>
        <div className={`${compact ? 'text-2xl mb-2' : 'text-4xl mb-3'}`}>🔍</div>
        <div className={`text-gray-700 dark:text-gray-300 font-medium ${compact ? 'text-xs mb-1' : 'mb-2'}`}>No Changes Preview Available</div>
        <div className={`text-gray-500 dark:text-gray-400 ${compact ? 'text-[10px]' : 'text-sm'}`}>
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
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      {/* Summary Header */}
      <div className={`bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/30 dark:to-indigo-900/30 border-b border-gray-200 dark:border-gray-700 ${compact ? 'p-2' : 'p-4'}`}>
        <div className={`flex items-center justify-between ${compact ? 'mb-2' : 'mb-3'}`}>
          <h3 className={`font-semibold text-gray-900 dark:text-white ${compact ? 'text-xs' : ''}`}>
            {compact ? 'Changes' : 'Infrastructure Changes Preview'}
          </h3>
          {showSelection && (
            <div className="flex items-center gap-2">
              <button
                onClick={selectAll}
                className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300`}
              >
                All
              </button>
              <button
                onClick={deselectAll}
                className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300`}
              >
                None
              </button>
              <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-gray-600 dark:text-gray-400`}>
                {selectedCount}/{localChanges.length}
              </span>
            </div>
          )}
        </div>
        <div className={`flex flex-wrap ${compact ? 'gap-1.5' : 'gap-3'}`}>
          {createCount > 0 && (
            <div className={`bg-white dark:bg-gray-800 border border-green-200 dark:border-green-800 rounded ${compact ? 'px-2 py-1' : 'p-3'} text-center flex items-center gap-1`}>
              <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-green-700 dark:text-green-400`}>{createCount}</span>
              <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-green-600 dark:text-green-500 font-medium`}>Create</span>
            </div>
          )}
          {modifyCount > 0 && (
            <div className={`bg-white dark:bg-gray-800 border border-yellow-200 dark:border-yellow-800 rounded ${compact ? 'px-2 py-1' : 'p-3'} text-center flex items-center gap-1`}>
              <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-yellow-700 dark:text-yellow-400`}>{modifyCount}</span>
              <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-yellow-600 dark:text-yellow-500 font-medium`}>Modify</span>
            </div>
          )}
          {deleteCount > 0 && (
            <div className={`bg-white dark:bg-gray-800 border border-red-200 dark:border-red-800 rounded ${compact ? 'px-2 py-1' : 'p-3'} text-center flex items-center gap-1`}>
              <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-red-700 dark:text-red-400`}>{deleteCount}</span>
              <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-red-600 dark:text-red-500 font-medium`}>Delete</span>
            </div>
          )}
          {noChangeCount > 0 && (
            <div className={`bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded ${compact ? 'px-2 py-1' : 'p-3'} text-center flex items-center gap-1`}>
              <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-gray-700 dark:text-gray-400`}>{noChangeCount}</span>
              <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-gray-600 dark:text-gray-500 font-medium`}>No Change</span>
            </div>
          )}
        </div>
      </div>

      {/* Changes List */}
      <div className={`divide-y divide-gray-200 dark:divide-gray-700 ${compact ? 'max-h-48' : 'max-h-96'} overflow-y-auto`}>
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
                  <div className={`${compact ? 'px-2 py-1.5' : 'px-3 py-3'} flex items-center`}>
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => toggleResourceSelection(change.resourceName)}
                      className={`${compact ? 'w-3 h-3' : 'w-4 h-4'} text-blue-600 border-gray-300 rounded focus:ring-blue-500`}
                      onClick={(e) => e.stopPropagation()}
                    />
                  </div>
                )}
                <button
                  onClick={() => hasDetails && toggleResource(change.resourceName)}
                  className={`flex-1 ${compact ? 'px-2 py-1.5' : 'px-4 py-3'} text-left hover:bg-opacity-75 transition-colors`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <span className={`font-mono ${compact ? 'text-sm' : 'text-lg'} font-bold`}>
                        {getChangeTypeIcon(change.changeType)}
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className={`font-medium ${compact ? 'text-xs truncate' : ''}`} title={change.resourceName}>{change.resourceName}</div>
                        <div className={`${compact ? 'text-[10px]' : 'text-sm'} opacity-75 truncate`} title={change.resourceType}>{change.resourceType}</div>
                      </div>
                    </div>
                    <div className="flex items-center space-x-2 flex-shrink-0">
                      <span className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} font-semibold rounded`}>
                        {getChangeTypeLabel(change.changeType)}
                      </span>
                      {hasDetails && (
                        <span className={compact ? 'text-xs' : 'text-sm'}>
                          {isExpanded ? '▼' : '▶'}
                        </span>
                      )}
                    </div>
                  </div>
                </button>
              </div>

              {/* Expanded Details */}
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
        })}
      </div>

      {/* Warning for destructive changes */}
      {deleteCount > 0 && (
        <div className={`bg-red-50 dark:bg-red-900/30 border-t border-red-200 dark:border-red-800 ${compact ? 'p-2' : 'p-4'}`}>
          <div className="flex items-start">
            <span className={compact ? 'text-sm mr-2' : 'text-2xl mr-3'}>⚠️</span>
            <div>
              <div className={`font-semibold text-red-900 dark:text-red-300 ${compact ? 'text-xs' : 'mb-1'}`}>Warning: Destructive Changes</div>
              <div className={`${compact ? 'text-[10px]' : 'text-sm'} text-red-800 dark:text-red-400`}>
                {deleteCount} resource{deleteCount > 1 ? 's' : ''} will be deleted. This action cannot be undone.
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
          <div className={`bg-yellow-50 dark:bg-yellow-900/30 border-t border-yellow-200 dark:border-yellow-800 ${compact ? 'p-2' : 'p-4'}`}>
            <div className="flex items-start">
              <span className={compact ? 'text-sm mr-2' : 'text-2xl mr-3'}>⚠️</span>
              <div>
                <div className={`font-semibold text-yellow-900 dark:text-yellow-300 ${compact ? 'text-xs' : 'mb-1'}`}>Dependency Warning</div>
                <div className={`${compact ? 'text-[10px]' : 'text-sm'} text-yellow-800 dark:text-yellow-400`}>
                  App Service needs {missingDeps.join(' and ')} selected.
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
