import { useMemo, useState } from 'react';

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
}

function ResourceGrid({ resources, loading, onRefresh, compact = false, viewMode = 'grid' }: ResourceGridProps) {
  const [expandedResource, setExpandedResource] = useState<string | null>(null);
  const [localViewMode, setLocalViewMode] = useState<'grid' | 'table'>(viewMode);
  const [groupByType, setGroupByType] = useState(false);
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set());

  // Group resources by type
  const groupedResources = useMemo(() => {
    if (!resources || !groupByType) return null;
    const groups: Record<string, AzureResource[]> = {};
    resources.forEach(resource => {
      const typeKey = resource.type.split('/').pop() || resource.type;
      if (!groups[typeKey]) {
        groups[typeKey] = [];
      }
      groups[typeKey].push(resource);
    });
    return groups;
  }, [resources, groupByType]);

  const toggleGroupCollapse = (groupKey: string) => {
    setCollapsedGroups(prev => {
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
    if (groupedResources) {
      setCollapsedGroups(new Set(Object.keys(groupedResources)));
    }
  };

  const expandAll = () => {
    setCollapsedGroups(new Set());
  };

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

  if (loading) {
    return (
      <div className="border border-gray-200 rounded-lg p-8">
        <div className="flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mr-4"></div>
          <div className="text-gray-700">Loading Azure resources...</div>
        </div>
      </div>
    );
  }

  if (!resources || resources.length === 0) {
    return (
      <div className="border border-gray-200 rounded-lg p-8 text-center">
        <div className="text-4xl mb-3">☁️</div>
        <div className="text-gray-700 font-medium mb-2">No Resources Found</div>
        <div className="text-gray-500 text-sm mb-4">
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

  // Table view for compact display
  const renderTableView = () => (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
      <table className="w-full text-xs">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th className="px-3 py-2 text-left font-medium text-gray-700 dark:text-gray-300">Resource</th>
            <th className="px-3 py-2 text-left font-medium text-gray-700 dark:text-gray-300">Type</th>
            <th className="px-3 py-2 text-left font-medium text-gray-700 dark:text-gray-300">Status</th>
            <th className="px-3 py-2 text-left font-medium text-gray-700 dark:text-gray-300">Region</th>
            {!compact && <th className="px-3 py-2 text-left font-medium text-gray-700 dark:text-gray-300">Cost</th>}
            <th className="px-3 py-2 text-right font-medium text-gray-700 dark:text-gray-300">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
          {resources.map((resource) => (
            <tr key={resource.id} className="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td className="px-3 py-2">
                <div className="flex items-center gap-2">
                  <span>{getResourceIcon(resource.type)}</span>
                  <span className="font-medium text-gray-900 dark:text-white truncate max-w-[150px]" title={resource.name}>
                    {resource.name}
                  </span>
                </div>
              </td>
              <td className="px-3 py-2 text-gray-600 dark:text-gray-400 truncate max-w-[120px]" title={resource.type}>
                {resource.type.split('/').pop()}
              </td>
              <td className="px-3 py-2">
                <span className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium ${getStatusColor(resource.status)}`}>
                  {getStatusIcon(resource.status)} {resource.status}
                </span>
              </td>
              <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{resource.region}</td>
              {!compact && (
                <td className="px-3 py-2 text-gray-900 dark:text-white font-medium">
                  {resource.costToday !== undefined ? formatCost(resource.costToday) : '-'}
                </td>
              )}
              <td className="px-3 py-2 text-right">
                <button
                  onClick={() => openInPortal(resource.id)}
                  className="text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300"
                  title="View in Azure Portal"
                >
                  🔗
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  // Grid view (original, with compact option)
  const renderGridView = () => (
    <div className={`grid ${compact ? 'grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2' : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4'}`}>
      {resources.map((resource) => {
        const isExpanded = expandedResource === resource.id;

        return (
          <div
            key={resource.id}
            className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 hover:shadow-md transition-shadow overflow-hidden"
          >
            {/* Card Header */}
            <div className={compact ? 'p-2' : 'p-4'}>
              <div className={`flex items-start justify-between ${compact ? 'mb-1' : 'mb-3'}`}>
                <div className="flex items-center min-w-0">
                  <span className={compact ? 'text-lg mr-1.5' : 'text-2xl mr-2'}>
                    {getResourceIcon(resource.type)}
                  </span>
                  <div className="min-w-0">
                    <div className={`font-medium text-gray-900 dark:text-white ${compact ? 'text-xs truncate' : 'text-sm'}`} title={resource.name}>
                      {resource.name}
                    </div>
                    <div className={`text-gray-500 dark:text-gray-400 ${compact ? 'text-[10px]' : 'text-xs mt-0.5'} truncate`}>
                      {resource.type.split('/').pop()}
                    </div>
                  </div>
                </div>
              </div>

              {/* Status Badge */}
              <div className={`flex items-center justify-between ${compact ? 'mb-1' : 'mb-3'}`}>
                <span
                  className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} font-medium rounded-full ${getStatusColor(resource.status)}`}
                >
                  {getStatusIcon(resource.status)} {resource.status}
                </span>
                <span className={`text-gray-500 dark:text-gray-400 ${compact ? 'text-[10px]' : 'text-xs'}`}>{resource.region}</span>
              </div>

              {/* Cost - only in non-compact mode */}
              {!compact && resource.costToday !== undefined && (
                <div className="mb-3 bg-blue-50 dark:bg-blue-900/30 border border-blue-100 dark:border-blue-800 rounded p-2">
                  <div className="text-xs text-blue-600 dark:text-blue-400 font-medium">Cost (Today)</div>
                  <div className="text-lg font-bold text-blue-900 dark:text-blue-300">
                    {formatCost(resource.costToday)}
                  </div>
                </div>
              )}

              {/* Last Updated - only in non-compact mode */}
              {!compact && resource.lastUpdated && (
                <div className="text-xs text-gray-500 dark:text-gray-400 mb-3">
                  Updated: {resource.lastUpdated}
                </div>
              )}

              {/* Actions */}
              <div className={`flex gap-1 ${compact ? 'mt-1' : ''}`}>
                <button
                  onClick={() => openInPortal(resource.id)}
                  className={`flex-1 ${compact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-2'} bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors`}
                >
                  {compact ? '🔗' : 'View in Portal'}
                </button>
                {!compact && resource.properties && Object.keys(resource.properties).length > 0 && (
                  <button
                    onClick={() => setExpandedResource(isExpanded ? null : resource.id)}
                    className="text-xs px-3 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
                  >
                    {isExpanded ? 'Hide' : 'Details'}
                  </button>
                )}
              </div>
            </div>

            {/* Expanded Properties - only in non-compact mode */}
            {!compact && isExpanded && resource.properties && (
              <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 p-4">
                <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Properties:
                </div>
                <div className="space-y-1">
                  {Object.entries(resource.properties).map(([key, value]) => (
                    <div key={key} className="flex justify-between text-xs">
                      <span className="text-gray-600 dark:text-gray-400">{key}:</span>
                      <span className="text-gray-900 dark:text-white font-medium">{value}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );

  // Render a single resource card (reusable for grouped view)
  const renderResourceCard = (resource: AzureResource, isCompact: boolean = compact) => {
    const isExpanded = expandedResource === resource.id;
    return (
      <div
        key={resource.id}
        className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 hover:shadow-md transition-shadow overflow-hidden"
      >
        <div className={isCompact ? 'p-2' : 'p-4'}>
          <div className={`flex items-start justify-between ${isCompact ? 'mb-1' : 'mb-3'}`}>
            <div className="flex items-center min-w-0">
              <span className={isCompact ? 'text-lg mr-1.5' : 'text-2xl mr-2'}>
                {getResourceIcon(resource.type)}
              </span>
              <div className="min-w-0">
                <div className={`font-medium text-gray-900 dark:text-white ${isCompact ? 'text-xs truncate' : 'text-sm'}`} title={resource.name}>
                  {resource.name}
                </div>
                <div className={`text-gray-500 dark:text-gray-400 ${isCompact ? 'text-[10px]' : 'text-xs mt-0.5'} truncate`}>
                  {resource.type.split('/').pop()}
                </div>
              </div>
            </div>
          </div>
          <div className={`flex items-center justify-between ${isCompact ? 'mb-1' : 'mb-3'}`}>
            <span className={`${isCompact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} font-medium rounded-full ${getStatusColor(resource.status)}`}>
              {getStatusIcon(resource.status)} {resource.status}
            </span>
            <span className={`text-gray-500 dark:text-gray-400 ${isCompact ? 'text-[10px]' : 'text-xs'}`}>{resource.region}</span>
          </div>
          {!isCompact && resource.costToday !== undefined && (
            <div className="mb-3 bg-blue-50 dark:bg-blue-900/30 border border-blue-100 dark:border-blue-800 rounded p-2">
              <div className="text-xs text-blue-600 dark:text-blue-400 font-medium">Cost (Today)</div>
              <div className="text-lg font-bold text-blue-900 dark:text-blue-300">{formatCost(resource.costToday)}</div>
            </div>
          )}
          <div className={`flex gap-1 ${isCompact ? 'mt-1' : ''}`}>
            <button
              onClick={() => openInPortal(resource.id)}
              className={`flex-1 ${isCompact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-2'} bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors`}
            >
              {isCompact ? '🔗' : 'View in Portal'}
            </button>
            {!isCompact && resource.properties && Object.keys(resource.properties).length > 0 && (
              <button
                onClick={() => setExpandedResource(isExpanded ? null : resource.id)}
                className="text-xs px-3 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
              >
                {isExpanded ? 'Hide' : 'Details'}
              </button>
            )}
          </div>
        </div>
        {!isCompact && isExpanded && resource.properties && (
          <div className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 p-4">
            <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">Properties:</div>
            <div className="space-y-1">
              {Object.entries(resource.properties).map(([key, value]) => (
                <div key={key} className="flex justify-between text-xs">
                  <span className="text-gray-600 dark:text-gray-400">{key}:</span>
                  <span className="text-gray-900 dark:text-white font-medium">{value}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  };

  // Render grouped view with collapsible sections
  const renderGroupedView = () => {
    if (!groupedResources) return null;

    return (
      <div className="space-y-4">
        {Object.entries(groupedResources).map(([typeKey, groupResources]) => {
          const isCollapsed = collapsedGroups.has(typeKey);
          const runningCount = groupResources.filter(r => r.status === 'running').length;
          const failedCount = groupResources.filter(r => r.status === 'failed').length;

          return (
            <div key={typeKey} className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
              {/* Group Header */}
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

              {/* Group Content */}
              {!isCollapsed && (
                <div className={`p-3 bg-white dark:bg-gray-800 grid ${compact ? 'grid-cols-2 md:grid-cols-3 gap-2' : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3'}`}>
                  {groupResources.map(resource => renderResourceCard(resource))}
                </div>
              )}
            </div>
          );
        })}
      </div>
    );
  };

  return (
    <div>
      {/* Header */}
      <div className={`flex items-center justify-between ${compact ? 'mb-2' : 'mb-4'}`}>
        <h3 className={`font-semibold text-gray-900 dark:text-white ${compact ? 'text-sm' : ''}`}>
          Azure Resources ({resources.length})
        </h3>
        <div className="flex items-center gap-2">
          {/* Group by Type Toggle */}
          <label className="flex items-center gap-1.5 text-xs text-gray-700 dark:text-gray-300 cursor-pointer" title="Group resources by type">
            <input
              type="checkbox"
              checked={groupByType}
              onChange={(e) => setGroupByType(e.target.checked)}
              className="rounded border-gray-300 dark:border-gray-600 w-3.5 h-3.5"
            />
            <span>Group</span>
          </label>

          {/* Collapse/Expand All - only when grouped */}
          {groupByType && groupedResources && (
            <div className="flex items-center gap-1">
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
          )}

          {/* View Mode Toggle - only when not grouped */}
          {!groupByType && (
            <div className="flex items-center bg-gray-100 dark:bg-gray-800 rounded p-0.5">
              <button
                onClick={() => setLocalViewMode('grid')}
                className={`px-2 py-1 text-xs rounded transition-colors ${
                  localViewMode === 'grid'
                    ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                    : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                }`}
                title="Grid view"
              >
                ▦
              </button>
              <button
                onClick={() => setLocalViewMode('table')}
                className={`px-2 py-1 text-xs rounded transition-colors ${
                  localViewMode === 'table'
                    ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                    : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                }`}
                title="Table view"
              >
                ≡
              </button>
            </div>
          )}
          {onRefresh && (
            <button
              onClick={onRefresh}
              className={`${compact ? 'text-[10px] px-2 py-1' : 'text-sm px-3 py-1'} bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 rounded transition-colors`}
            >
              🔄 {compact ? '' : 'Refresh'}
            </button>
          )}
        </div>
      </div>

      {/* Resource Display */}
      {groupByType ? renderGroupedView() : (localViewMode === 'table' ? renderTableView() : renderGridView())}

      {/* Summary Footer - compact version for compact mode */}
      <div className={`${compact ? 'mt-3 p-2' : 'mt-6 p-4'} bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700 rounded-lg`}>
        <div className={`flex items-center justify-center gap-4 ${compact ? 'text-xs' : ''}`}>
          <div className="flex items-center gap-1">
            <span className={`font-bold text-gray-900 dark:text-white ${compact ? '' : 'text-xl'}`}>{resources.length}</span>
            <span className="text-gray-600 dark:text-gray-400">Total</span>
          </div>
          <div className="flex items-center gap-1">
            <span className={`font-bold text-green-700 dark:text-green-400 ${compact ? '' : 'text-xl'}`}>
              {resources.filter((r) => r.status === 'running').length}
            </span>
            <span className="text-gray-600 dark:text-gray-400">Running</span>
          </div>
          <div className="flex items-center gap-1">
            <span className={`font-bold text-yellow-700 dark:text-yellow-400 ${compact ? '' : 'text-xl'}`}>
              {resources.filter((r) => r.status === 'warning').length}
            </span>
            <span className="text-gray-600 dark:text-gray-400">Warnings</span>
          </div>
          <div className="flex items-center gap-1">
            <span className={`font-bold text-red-700 dark:text-red-400 ${compact ? '' : 'text-xl'}`}>
              {resources.filter((r) => r.status === 'failed').length}
            </span>
            <span className="text-gray-600 dark:text-gray-400">Failed</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ResourceGrid;
