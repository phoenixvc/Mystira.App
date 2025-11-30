import { useState } from 'react';
import {
    ResourceGridHeader,
    ResourceGridSummary,
    ResourceGridView,
    ResourceGroupedView,
    ResourceTableView,
} from './components';

interface AzureResource {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'warning' | 'failed' | 'unknown';
  region: string;
  costToday?: number;
  lastUpdated?: string;
  properties?: Record<string, string>;
}

interface ResourceGridProps {
  resources?: AzureResource[];
  loading?: boolean;
  onRefresh?: () => void;
  compact?: boolean;
  viewMode?: 'grid' | 'table';
  onDelete?: (resourceId: string) => Promise<void>;
}

function ResourceGrid({
  resources,
  loading,
  onRefresh,
  compact = false,
  viewMode = 'grid',
  onDelete,
}: ResourceGridProps) {
  const [localViewMode, setLocalViewMode] = useState<'grid' | 'table'>(viewMode);
  const [groupByType, setGroupByType] = useState(false);
  const [deletingResource, setDeletingResource] = useState<string | null>(null);

  const handleDelete = async (resourceId: string, resourceName: string) => {
    const confirmDelete = confirm(
      `⚠️ WARNING: Delete Resource\n\n` +
        `Are you sure you want to delete this resource?\n\n` +
        `Name: ${resourceName}\n` +
        `ID: ${resourceId}\n\n` +
        `This action cannot be undone!`
    );

    if (!confirmDelete || !onDelete) return;

    setDeletingResource(resourceId);
    try {
      await onDelete(resourceId);
      if (onRefresh) {
        onRefresh();
      }
    } catch (error) {
      alert(`Failed to delete resource: ${error}`);
    } finally {
      setDeletingResource(null);
    }
  };

  if (loading) {
    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-8 bg-white dark:bg-gray-800">
        <div className="flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400 mr-4"></div>
          <div className="text-gray-700 dark:text-gray-300">Loading Azure resources...</div>
        </div>
      </div>
    );
  }

  if (!resources || resources.length === 0) {
    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg p-8 text-center bg-white dark:bg-gray-800">
        <div className="text-4xl mb-3">☁️</div>
        <div className="text-gray-700 dark:text-gray-300 font-medium mb-2">No Resources Found</div>
        <div className="text-gray-500 dark:text-gray-400 text-sm mb-4">
          Deploy infrastructure or check your Azure connection
        </div>
        {onRefresh && (
          <button
            onClick={onRefresh}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Refresh Resources
          </button>
        )}
      </div>
    );
  }

  return (
    <div>
      <ResourceGridHeader
        resourcesCount={resources.length}
        compact={compact}
        groupByType={groupByType}
        viewMode={localViewMode}
        onRefresh={onRefresh}
        onViewModeChange={setLocalViewMode}
        onGroupByTypeChange={setGroupByType}
      />

      {groupByType ? (
        <ResourceGroupedView
          resources={resources}
          compact={compact}
          onDelete={onDelete ? (id, name) => handleDelete(id, name) : undefined}
          deletingResource={deletingResource}
        />
      ) : localViewMode === 'table' ? (
        <ResourceTableView
          resources={resources}
          compact={compact}
          onDelete={onDelete ? (id, name) => handleDelete(id, name) : undefined}
          deletingResource={deletingResource}
        />
      ) : (
        <ResourceGridView
          resources={resources}
          compact={compact}
          onDelete={onDelete ? (id, name) => handleDelete(id, name) : undefined}
          deletingResource={deletingResource}
        />
      )}

      <div className={`mt-4 pt-4 border-t border-gray-200 dark:border-gray-700 ${compact ? 'text-xs' : ''}`}>
        <ResourceGridSummary resources={resources} compact={compact} />
      </div>
    </div>
  );
}

export default ResourceGrid;

