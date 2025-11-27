import { useState } from 'react';

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
  onDelete?: (resourceId: string) => Promise<void>;
}

function ResourceGrid({ resources, loading, onRefresh, onDelete }: ResourceGridProps) {
  const [expandedResource, setExpandedResource] = useState<string | null>(null);
  const [deletingResource, setDeletingResource] = useState<string | null>(null);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'running':
        return 'text-green-700 bg-green-100';
      case 'stopped':
        return 'text-gray-700 bg-gray-100';
      case 'warning':
        return 'text-yellow-700 bg-yellow-100';
      case 'failed':
        return 'text-red-700 bg-red-100';
      default:
        return 'text-gray-700 bg-gray-100';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'running':
        return '✅';
      case 'stopped':
        return '⏸️';
      case 'warning':
        return '⚠️';
      case 'failed':
        return '❌';
      default:
        return '❓';
    }
  };

  const getResourceIcon = (type: string) => {
    if (type.includes('cosmos') || type.includes('database')) return '🗄️';
    if (type.includes('storage')) return '📦';
    if (type.includes('app') || type.includes('site')) return '🌐';
    if (type.includes('analytics')) return '📊';
    if (type.includes('insights')) return '📈';
    if (type.includes('communication')) return '💬';
    return '📋';
  };

  const formatCost = (cost: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
    }).format(cost);
  };

  const openInPortal = (resourceId: string) => {
    const portalUrl = `https://portal.azure.com/#@/resource${resourceId}`;
    // In Tauri, we would use shell.open(portalUrl)
    console.log('Open in portal:', portalUrl);
    alert(`Would open Azure Portal for resource: ${resourceId}`);
  };

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
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-semibold text-gray-900 dark:text-gray-100">
          Azure Resources ({resources.length})
        </h3>
        {onRefresh && (
          <button
            onClick={onRefresh}
            className="text-sm px-3 py-1 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded transition-colors"
          >
            🔄 Refresh
          </button>
        )}
      </div>

      {/* Resource Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {resources.map((resource) => {
          const isExpanded = expandedResource === resource.id;

          return (
            <div
              key={resource.id}
              className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 hover:shadow-md transition-shadow overflow-hidden"
            >
              {/* Card Header */}
              <div className="p-4">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center">
                    <span className="text-2xl mr-2">
                      {getResourceIcon(resource.type)}
                    </span>
                    <div>
                      <div className="font-medium text-gray-900 dark:text-gray-100 text-sm">
                        {resource.name}
                      </div>
                      <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                        {resource.type.split('/').pop()}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Status Badge */}
                <div className="flex items-center justify-between mb-3">
                  <span
                    className={`text-xs font-medium px-2 py-1 rounded-full ${getStatusColor(
                      resource.status
                    )}`}
                  >
                    {getStatusIcon(resource.status)} {resource.status}
                  </span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">{resource.region}</span>
                </div>

                {/* Cost */}
                {resource.costToday !== undefined && (
                  <div className="mb-3 bg-blue-50 border border-blue-100 rounded p-2">
                    <div className="text-xs text-blue-600 font-medium">Cost (Today)</div>
                    <div className="text-lg font-bold text-blue-900">
                      {formatCost(resource.costToday)}
                    </div>
                  </div>
                )}

                {/* Last Updated */}
                {resource.lastUpdated && (
                  <div className="text-xs text-gray-500 mb-3">
                    Updated: {resource.lastUpdated}
                  </div>
                )}

                {/* Actions */}
                <div className="flex gap-2">
                  <button
                    onClick={() => openInPortal(resource.id)}
                    className="flex-1 text-xs px-3 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
                  >
                    View in Portal
                  </button>
                  {resource.properties && Object.keys(resource.properties).length > 0 && (
                    <button
                      onClick={() =>
                        setExpandedResource(isExpanded ? null : resource.id)
                      }
                      className="text-xs px-3 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
                    >
                      {isExpanded ? 'Hide' : 'Details'}
                    </button>
                  )}
                  {onDelete && (
                    <button
                      onClick={() => handleDelete(resource.id, resource.name)}
                      disabled={deletingResource === resource.id}
                      className="text-xs px-3 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {deletingResource === resource.id ? 'Deleting...' : '🗑️'}
                    </button>
                  )}
                </div>
              </div>

              {/* Expanded Properties */}
              {isExpanded && resource.properties && (
                <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 p-4">
                  <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Properties:
                  </div>
                  <div className="space-y-1">
                    {Object.entries(resource.properties).map(([key, value]) => (
                      <div key={key} className="flex justify-between text-xs">
                        <span className="text-gray-600 dark:text-gray-400">{key}:</span>
                        <span className="text-gray-900 dark:text-gray-100 font-medium">{value}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Summary Footer */}
      <div className="mt-6 p-4 bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg">
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4 text-center">
          <div>
            <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
              {resources.length}
            </div>
            <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">Total Resources</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-green-700 dark:text-green-400">
              {resources.filter((r) => r.status === 'running').length}
            </div>
            <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">Running</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-gray-700 dark:text-gray-300">
              {resources.filter((r) => r.status === 'stopped').length}
            </div>
            <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">Stopped</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-yellow-700 dark:text-yellow-400">
              {resources.filter((r) => r.status === 'warning').length}
            </div>
            <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">Warnings</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-red-700 dark:text-red-400">
              {resources.filter((r) => r.status === 'failed').length}
            </div>
            <div className="text-xs text-gray-600 dark:text-gray-400 mt-1">Failed</div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ResourceGrid;
