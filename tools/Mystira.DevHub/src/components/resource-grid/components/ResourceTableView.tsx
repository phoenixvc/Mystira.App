import { formatCost, getResourceIcon, getStatusColor, getStatusIcon, openInPortal } from '../utils/resourceUtils';

interface AzureResource {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'warning' | 'failed' | 'unknown';
  region: string;
  costToday?: number;
}

interface ResourceTableViewProps {
  resources: AzureResource[];
  compact: boolean;
  onDelete?: (resourceId: string, resourceName: string) => Promise<void>;
  deletingResource?: string | null;
}

export function ResourceTableView({
  resources,
  compact,
  onDelete,
  deletingResource,
}: ResourceTableViewProps) {
  return (
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
                  <span
                    className="font-medium text-gray-900 dark:text-white truncate max-w-[150px]"
                    title={resource.name}
                  >
                    {resource.name}
                  </span>
                </div>
              </td>
              <td
                className="px-3 py-2 text-gray-600 dark:text-gray-400 truncate max-w-[120px]"
                title={resource.type}
              >
                {resource.type.split('/').pop()}
              </td>
              <td className="px-3 py-2">
                <span
                  className={`inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium ${getStatusColor(resource.status)}`}
                >
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
                <div className="flex items-center justify-end gap-1">
                  <button
                    onClick={() => openInPortal(resource.id)}
                    className="text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300"
                    title="View in Azure Portal"
                  >
                    🔗
                  </button>
                  {onDelete && (
                    <button
                      onClick={() => onDelete(resource.id, resource.name)}
                      disabled={deletingResource === resource.id}
                      className="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 disabled:opacity-50 disabled:cursor-not-allowed"
                      title="Delete resource"
                    >
                      {deletingResource === resource.id ? '⏳' : '🗑️'}
                    </button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

