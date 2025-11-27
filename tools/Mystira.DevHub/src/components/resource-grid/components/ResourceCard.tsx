import { useState } from 'react';
import { formatCost, getResourceIcon, getStatusColor, getStatusIcon, openInPortal } from '../utils/resourceUtils';

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

interface ResourceCardProps {
  resource: AzureResource;
  compact: boolean;
  onDelete?: (resourceId: string, resourceName: string) => Promise<void>;
  deletingResource?: string | null;
}

export function ResourceCard({ resource, compact, onDelete, deletingResource }: ResourceCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 hover:shadow-md transition-shadow overflow-hidden">
      <div className={compact ? 'p-2' : 'p-4'}>
        <div className={`flex items-start justify-between ${compact ? 'mb-1' : 'mb-3'}`}>
          <div className="flex items-center min-w-0">
            <span className={compact ? 'text-lg mr-1.5' : 'text-2xl mr-2'}>
              {getResourceIcon(resource.type)}
            </span>
            <div className="min-w-0">
              <div
                className={`font-medium text-gray-900 dark:text-white ${compact ? 'text-xs truncate' : 'text-sm'}`}
                title={resource.name}
              >
                {resource.name}
              </div>
              <div className={`text-gray-500 dark:text-gray-400 ${compact ? 'text-[10px]' : 'text-xs mt-0.5'} truncate`}>
                {resource.type.split('/').pop()}
              </div>
            </div>
          </div>
        </div>
        <div className={`flex items-center justify-between ${compact ? 'mb-1' : 'mb-3'}`}>
          <span
            className={`${compact ? 'text-[10px] px-1.5 py-0.5' : 'text-xs px-2 py-1'} font-medium rounded-full ${getStatusColor(resource.status)}`}
          >
            {getStatusIcon(resource.status)} {resource.status}
          </span>
          <span className={`text-gray-500 dark:text-gray-400 ${compact ? 'text-[10px]' : 'text-xs'}`}>
            {resource.region}
          </span>
        </div>
        {!compact && resource.costToday !== undefined && (
          <div className="mb-3 bg-blue-50 dark:bg-blue-900/30 border border-blue-100 dark:border-blue-800 rounded p-2">
            <div className="text-xs text-blue-600 dark:text-blue-400 font-medium">Cost (Today)</div>
            <div className="text-lg font-bold text-blue-900 dark:text-blue-300">{formatCost(resource.costToday)}</div>
          </div>
        )}
        <div className={`flex gap-1 ${compact ? 'mt-1' : ''}`}>
          <button
            onClick={() => openInPortal(resource.id)}
            className={`flex-1 ${compact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-2'} bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors`}
          >
            {compact ? '🔗' : 'View in Portal'}
          </button>
          {!compact && resource.properties && Object.keys(resource.properties).length > 0 && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-xs px-3 py-2 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            >
              {isExpanded ? 'Hide' : 'Details'}
            </button>
          )}
          {onDelete && (
            <button
              onClick={() => onDelete(resource.id, resource.name)}
              disabled={deletingResource === resource.id}
              className={`${compact ? 'text-[10px] px-2 py-1' : 'text-xs px-3 py-2'} bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors`}
              title="Delete resource"
            >
              {deletingResource === resource.id ? '⏳' : '🗑️'}
            </button>
          )}
        </div>
      </div>
      {!compact && isExpanded && resource.properties && (
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
}

