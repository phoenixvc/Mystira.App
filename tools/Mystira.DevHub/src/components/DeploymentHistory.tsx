import { useState } from 'react';

interface DeploymentEvent {
  id: string;
  timestamp: string;
  action: 'validate' | 'preview' | 'deploy' | 'destroy';
  status: 'success' | 'failed' | 'in_progress';
  duration?: string;
  resourcesAffected?: number;
  user?: string;
  message?: string;
  githubUrl?: string;
  azureUrl?: string;
}

interface DeploymentHistoryProps {
  events?: DeploymentEvent[];
  loading?: boolean;
}

function DeploymentHistory({ events, loading }: DeploymentHistoryProps) {
  const [expandedEvent, setExpandedEvent] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'deploy' | 'validate' | 'preview' | 'destroy'>('all');

  const getActionIcon = (action: string) => {
    switch (action) {
      case 'validate':
        return '🔍';
      case 'preview':
        return '👁️';
      case 'deploy':
        return '🚀';
      case 'destroy':
        return '💥';
      default:
        return '📋';
    }
  };

  const getActionColor = (action: string) => {
    switch (action) {
      case 'validate':
        return 'text-blue-700 bg-blue-50 border-blue-200';
      case 'preview':
        return 'text-purple-700 bg-purple-50 border-purple-200';
      case 'deploy':
        return 'text-green-700 bg-green-50 border-green-200';
      case 'destroy':
        return 'text-red-700 bg-red-50 border-red-200';
      default:
        return 'text-gray-700 bg-gray-50 border-gray-200';
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'success':
        return <span className="text-xs px-2 py-1 bg-green-100 text-green-800 rounded-full">✓ Success</span>;
      case 'failed':
        return <span className="text-xs px-2 py-1 bg-red-100 text-red-800 rounded-full">✗ Failed</span>;
      case 'in_progress':
        return <span className="text-xs px-2 py-1 bg-yellow-100 text-yellow-800 rounded-full">⏳ In Progress</span>;
      default:
        return <span className="text-xs px-2 py-1 bg-gray-100 text-gray-800 rounded-full">? Unknown</span>;
    }
  };

  const getActionLabel = (action: string) => {
    switch (action) {
      case 'validate':
        return 'Validate Templates';
      case 'preview':
        return 'Preview Changes';
      case 'deploy':
        return 'Deploy Infrastructure';
      case 'destroy':
        return 'Destroy Infrastructure';
      default:
        return action;
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (loading) {
    return (
      <div className="border border-gray-200 rounded-lg p-8">
        <div className="flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mr-4"></div>
          <div className="text-gray-700">Loading deployment history...</div>
        </div>
      </div>
    );
  }

  if (!events || events.length === 0) {
    return (
      <div className="border border-gray-200 rounded-lg p-8 text-center">
        <div className="text-4xl mb-3">📜</div>
        <div className="text-gray-700 font-medium mb-2">No Deployment History</div>
        <div className="text-gray-500 text-sm">
          Deployment events will appear here once you start infrastructure operations
        </div>
      </div>
    );
  }

  const filteredEvents = filter === 'all' ? events : events.filter((e) => e.action === filter);

  return (
    <div>
      {/* Filter Tabs */}
      <div className="flex gap-2 mb-4 border-b border-gray-200 pb-2">
        {['all', 'deploy', 'validate', 'preview', 'destroy'].map((filterOption) => (
          <button
            key={filterOption}
            onClick={() => setFilter(filterOption as any)}
            className={`px-3 py-1 text-sm rounded-t transition-colors ${
              filter === filterOption
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            {filterOption.charAt(0).toUpperCase() + filterOption.slice(1)}
            {filterOption !== 'all' && ` (${events.filter((e) => e.action === filterOption).length})`}
          </button>
        ))}
      </div>

      {/* Timeline */}
      <div className="space-y-3 max-h-96 overflow-y-auto">
        {filteredEvents.map((event, index) => {
          const isExpanded = expandedEvent === event.id;
          const hasDetails = event.message || event.githubUrl || event.azureUrl;

          return (
            <div
              key={event.id}
              className={`border rounded-lg overflow-hidden ${getActionColor(event.action)}`}
            >
              {/* Event Header */}
              <div className="p-4">
                <div className="flex items-start justify-between">
                  <div className="flex items-start space-x-3">
                    {/* Timeline Dot */}
                    <div className="relative">
                      <div className="text-2xl">{getActionIcon(event.action)}</div>
                      {index < filteredEvents.length - 1 && (
                        <div className="absolute left-1/2 top-8 w-0.5 h-8 bg-gray-300 -ml-px"></div>
                      )}
                    </div>

                    {/* Event Info */}
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="font-semibold">
                          {getActionLabel(event.action)}
                        </span>
                        {getStatusBadge(event.status)}
                      </div>

                      <div className="text-sm opacity-75 mb-2">
                        📅 {formatTimestamp(event.timestamp)}
                        {event.duration && <span className="ml-3">⏱️ {event.duration}</span>}
                      </div>

                      {event.resourcesAffected !== undefined && (
                        <div className="text-sm">
                          <strong>Resources Affected:</strong> {event.resourcesAffected}
                        </div>
                      )}

                      {event.user && (
                        <div className="text-sm opacity-75">
                          👤 {event.user}
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Expand Button */}
                  {hasDetails && (
                    <button
                      onClick={() => setExpandedEvent(isExpanded ? null : event.id)}
                      className="ml-2 px-2 py-1 text-xs bg-white bg-opacity-50 hover:bg-opacity-75 rounded transition-colors"
                    >
                      {isExpanded ? 'Less' : 'Details'}
                    </button>
                  )}
                </div>

                {/* Expanded Details */}
                {isExpanded && hasDetails && (
                  <div className="mt-3 pt-3 border-t border-current border-opacity-20">
                    {event.message && (
                      <div className="mb-3 text-sm bg-white bg-opacity-50 rounded p-2">
                        <strong>Message:</strong>
                        <div className="mt-1">{event.message}</div>
                      </div>
                    )}

                    <div className="flex gap-2">
                      {event.githubUrl && (
                        <a
                          href={event.githubUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-xs px-3 py-1 bg-gray-900 text-white rounded hover:bg-gray-800 transition-colors"
                        >
                          View in GitHub →
                        </a>
                      )}
                      {event.azureUrl && (
                        <a
                          href={event.azureUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-xs px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 transition-colors"
                        >
                          View in Azure →
                        </a>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Summary Footer */}
      {filteredEvents.length === 0 && filter !== 'all' && (
        <div className="text-center text-gray-500 text-sm py-8">
          No {filter} operations found in history
        </div>
      )}
    </div>
  );
}

export default DeploymentHistory;
