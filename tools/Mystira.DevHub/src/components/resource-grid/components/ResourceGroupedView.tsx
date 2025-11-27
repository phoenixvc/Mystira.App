import { useState, useMemo } from 'react';
import { getResourceIcon } from '../utils/resourceUtils';
import { ResourceCard } from './ResourceCard';

interface AzureResource {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'warning' | 'failed' | 'unknown';
  region: string;
  costToday?: number;
  properties?: Record<string, string>;
}

interface ResourceGroupedViewProps {
  resources: AzureResource[];
  compact: boolean;
  onDelete?: (resourceId: string, resourceName: string) => Promise<void>;
  deletingResource?: string | null;
}

export function ResourceGroupedView({
  resources,
  compact,
  onDelete,
  deletingResource,
}: ResourceGroupedViewProps) {
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set());

  const groupedResources = useMemo(() => {
    const groups: Record<string, AzureResource[]> = {};
    resources.forEach((resource) => {
      const typeKey = resource.type.split('/').pop() || resource.type;
      if (!groups[typeKey]) {
        groups[typeKey] = [];
      }
      groups[typeKey].push(resource);
    });
    return groups;
  }, [resources]);

  const toggleGroupCollapse = (groupKey: string) => {
    setCollapsedGroups((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(groupKey)) {
        newSet.delete(groupKey);
      } else {
        newSet.add(groupKey);
      }
      return newSet;
    });
  };

  const collapseAll = () => {
    setCollapsedGroups(new Set(Object.keys(groupedResources)));
  };

  const expandAll = () => {
    setCollapsedGroups(new Set());
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs text-gray-600 dark:text-gray-400">Grouped by type</span>
        <div className="flex gap-1">
          <button
            onClick={collapseAll}
            className="px-2 py-1 text-[10px] bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 rounded transition-colors"
            title="Collapse all groups"
          >
            ▲
          </button>
          <button
            onClick={expandAll}
            className="px-2 py-1 text-[10px] bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-400 rounded transition-colors"
            title="Expand all groups"
          >
            ▼
          </button>
        </div>
      </div>
      {Object.entries(groupedResources).map(([typeKey, groupResources]) => {
        const isCollapsed = collapsedGroups.has(typeKey);
        const runningCount = groupResources.filter((r) => r.status === 'running').length;
        const failedCount = groupResources.filter((r) => r.status === 'failed').length;

        return (
          <div key={typeKey} className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleGroupCollapse(typeKey)}
              className="w-full flex items-center justify-between px-4 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            >
              <div className="flex items-center gap-2">
                <span className="text-sm">{isCollapsed ? '▶' : '▼'}</span>
                <span className="text-lg">{getResourceIcon(typeKey)}</span>
                <span className="font-medium text-gray-900 dark:text-white text-sm">{typeKey}</span>
                <span className="text-xs text-gray-500 dark:text-gray-400">({groupResources.length})</span>
              </div>
              <div className="flex items-center gap-2">
                {runningCount > 0 && (
                  <span className="text-xs text-green-600 dark:text-green-400">✓ {runningCount}</span>
                )}
                {failedCount > 0 && (
                  <span className="text-xs text-red-600 dark:text-red-400">✕ {failedCount}</span>
                )}
              </div>
            </button>
            {!isCollapsed && (
              <div
                className={`p-3 bg-white dark:bg-gray-800 grid ${
                  compact ? 'grid-cols-2 md:grid-cols-3 gap-2' : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3'
                }`}
              >
                {groupResources.map((resource) => (
                  <ResourceCard
                    key={resource.id}
                    resource={resource}
                    compact={compact}
                    onDelete={onDelete}
                    deletingResource={deletingResource}
                  />
                ))}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

