import { memo, useCallback, useEffect, useMemo, useState } from 'react';
import { useConnectionStore } from '../../stores/connectionStore';
import type { DashboardProps, QuickAction, RecentOperation } from '../../types';
import { OperationStatusBadge } from '../ui/feedback/components';

function Dashboard({ onNavigate }: DashboardProps) {
  // Use connection store instead of local state
  const { connections, testConnections } = useConnectionStore();

  const [recentOperations] = useState<RecentOperation[]>([
    {
      id: '1',
      type: 'export',
      title: 'Export Game Sessions',
      timestamp: new Date(Date.now() - 3600000).toISOString(),
      status: 'success',
      details: 'Exported 1,234 sessions to CSV',
    },
    {
      id: '2',
      type: 'stats',
      title: 'Scenario Statistics',
      timestamp: new Date(Date.now() - 7200000).toISOString(),
      status: 'success',
      details: 'Retrieved stats for 15 scenarios',
    },
    {
      id: '3',
      type: 'deploy',
      title: 'Deploy Infrastructure',
      timestamp: new Date(Date.now() - 86400000).toISOString(),
      status: 'success',
      details: '7 resources deployed successfully',
    },
    {
      id: '4',
      type: 'validate',
      title: 'Validate Bicep Templates',
      timestamp: new Date(Date.now() - 172800000).toISOString(),
      status: 'success',
      details: 'All templates validated',
    },
    {
      id: '5',
      type: 'migration',
      title: 'Migrate Content Bundles',
      timestamp: new Date(Date.now() - 259200000).toISOString(),
      status: 'failed',
      details: 'Connection timeout to destination',
    },
  ]);

  // Test connections on mount using the store
  useEffect(() => {
    testConnections();
  }, [testConnections]);

  // Memoize quick actions to prevent recreation on every render
  const quickActions: QuickAction[] = useMemo(() => [
    {
      id: 'export',
      title: 'Export Sessions',
      description: 'Export game sessions to CSV',
      icon: '📤',
      action: () => onNavigate('cosmos'),
      color: 'from-blue-400 to-blue-600',
    },
    {
      id: 'stats',
      title: 'View Statistics',
      description: 'Scenario completion analytics',
      icon: '📊',
      action: () => onNavigate('cosmos'),
      color: 'from-green-400 to-green-600',
    },
    {
      id: 'migrate',
      title: 'Run Migration',
      description: 'Migrate data between environments',
      icon: '🔄',
      action: () => onNavigate('migration'),
      color: 'from-purple-400 to-purple-600',
    },
    {
      id: 'validate',
      title: 'Validate Infrastructure',
      description: 'Check Bicep templates',
      icon: '🔍',
      action: () => onNavigate('infrastructure'),
      color: 'from-yellow-400 to-yellow-600',
    },
    {
      id: 'deploy',
      title: 'Deploy Infrastructure',
      description: 'Deploy to Azure via GitHub Actions',
      icon: '🚀',
      action: () => onNavigate('infrastructure'),
      color: 'from-red-400 to-red-600',
    },
    {
      id: 'bicep',
      title: 'View Bicep Files',
      description: 'Browse infrastructure templates',
      icon: '📄',
      action: () => onNavigate('infrastructure'),
      color: 'from-indigo-400 to-indigo-600',
    },
  ], [onNavigate]);

  // Memoize utility functions to prevent recreation
  const getStatusColor = useCallback((status: string) => {
    switch (status) {
      case 'connected':
        return 'text-green-700 bg-green-100 border-green-200';
      case 'disconnected':
        return 'text-red-700 bg-red-100 border-red-200';
      case 'checking':
        return 'text-yellow-700 bg-yellow-100 border-yellow-200';
      default:
        return 'text-gray-700 bg-gray-100 border-gray-200';
    }
  }, []);

  const getStatusIcon = useCallback((status: string) => {
    switch (status) {
      case 'connected':
        return '✓';
      case 'disconnected':
        return '✗';
      case 'checking':
        return '⏳';
      default:
        return '?';
    }
  }, []);

  const getOperationIcon = useCallback((type: string) => {
    switch (type) {
      case 'export':
        return '📤';
      case 'stats':
        return '📊';
      case 'deploy':
        return '🚀';
      case 'validate':
        return '🔍';
      case 'migration':
        return '🔄';
      default:
        return '📋';
    }
  }, []);

  // Status badge is now handled by OperationStatusBadge component

  const formatTimestamp = useCallback((timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diff = now.getTime() - date.getTime();

    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 60) {
      return `${minutes}m ago`;
    } else if (hours < 24) {
      return `${hours}h ago`;
    } else {
      return `${days}d ago`;
    }
  }, []);

  return (
    <main className="p-8" id="main-content">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <header className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">
            Welcome to Mystira DevHub
          </h1>
          <p className="text-gray-600 text-lg">
            Your central hub for development operations and data management
          </p>
        </header>

        {/* Connection Status */}
        <section className="mb-8" aria-labelledby="connection-status-heading">
          <h2 id="connection-status-heading" className="text-xl font-semibold text-gray-900 mb-4">
            Connection Status
          </h2>
          <div
            className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4"
            role="list"
            aria-label="Service connection statuses"
          >
            {connections.map((connection) => (
              <div
                key={connection.name}
                role="listitem"
                className={`border rounded-lg p-4 ${getStatusColor(connection.status)}`}
                aria-label={`${connection.name}: ${connection.status}`}
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center">
                    <span className="text-2xl mr-2" aria-hidden="true">{connection.icon}</span>
                    <span className="font-medium">{connection.name}</span>
                  </div>
                  <span
                    className="text-lg font-bold"
                    aria-label={connection.status}
                    role="status"
                  >
                    {getStatusIcon(connection.status)}
                  </span>
                </div>
                {connection.details && (
                  <div className="text-xs opacity-75 truncate">
                    {connection.details}
                  </div>
                )}
                {connection.error && (
                  <div className="text-xs opacity-75 truncate mt-1" role="alert">
                    {connection.error}
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>

        {/* Quick Actions */}
        <section className="mb-8" aria-labelledby="quick-actions-heading">
          <h2 id="quick-actions-heading" className="text-xl font-semibold text-gray-900 mb-4">
            Quick Actions
          </h2>
          <nav
            className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
            aria-label="Quick action buttons"
          >
            {quickActions.map((action) => (
              <button
                key={action.id}
                onClick={action.action}
                className={`relative overflow-hidden rounded-lg p-6 text-left transition-all transform hover:scale-105 hover:shadow-lg bg-gradient-to-br ${action.color} text-white group`}
                aria-label={`${action.title}: ${action.description}`}
              >
                <div className="absolute top-0 right-0 opacity-10 text-9xl transform translate-x-8 -translate-y-4" aria-hidden="true">
                  {action.icon}
                </div>
                <div className="relative z-10">
                  <div className="text-4xl mb-3" aria-hidden="true">{action.icon}</div>
                  <div className="text-xl font-bold mb-2">{action.title}</div>
                  <div className="text-sm opacity-90">{action.description}</div>
                </div>
                <div className="absolute bottom-0 right-0 transform translate-y-1 translate-x-1 opacity-0 group-hover:opacity-100 transition-opacity" aria-hidden="true">
                  <span className="text-white text-2xl">→</span>
                </div>
              </button>
            ))}
          </nav>
        </section>

        {/* Recent Operations */}
        <section className="mb-8" aria-labelledby="recent-operations-heading">
          <div className="flex items-center justify-between mb-4">
            <h2 id="recent-operations-heading" className="text-xl font-semibold text-gray-900">
              Recent Operations
            </h2>
            <button
              className="text-sm text-blue-600 hover:text-blue-700 font-medium"
              aria-label="View all recent operations"
            >
              View All →
            </button>
          </div>
          <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
            {recentOperations.length === 0 ? (
              <div className="p-8 text-center text-gray-500">
                No recent operations
              </div>
            ) : (
              <div className="divide-y divide-gray-200">
                {recentOperations.map((operation) => (
                  <div
                    key={operation.id}
                    className="p-4 hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3 flex-1">
                        <div className="text-2xl">{getOperationIcon(operation.type)}</div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-900">
                              {operation.title}
                            </span>
                            <OperationStatusBadge status={operation.status} size="sm" />
                          </div>
                          {operation.details && (
                            <div className="text-sm text-gray-600 mt-0.5">
                              {operation.details}
                            </div>
                          )}
                        </div>
                      </div>
                      <div className="text-sm text-gray-500 ml-4">
                        {formatTimestamp(operation.timestamp)}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </section>

        {/* System Info */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-6">
            <div className="text-3xl mb-3">💡</div>
            <div className="font-semibold text-gray-900 mb-2">Tips & Tricks</div>
            <div className="text-sm text-gray-700">
              Use Quick Actions above for common operations, or navigate via the sidebar for advanced features
            </div>
          </div>

          <div className="bg-gradient-to-br from-green-50 to-emerald-50 border border-green-200 rounded-lg p-6">
            <div className="text-3xl mb-3">📚</div>
            <div className="font-semibold text-gray-900 mb-2">Documentation</div>
            <div className="text-sm text-gray-700">
              Check the README in tools/Mystira.DevHub for complete setup and usage guides
            </div>
          </div>

          <div className="bg-gradient-to-br from-purple-50 to-pink-50 border border-purple-200 rounded-lg p-6">
            <div className="text-3xl mb-3">⚡</div>
            <div className="font-semibold text-gray-900 mb-2">Performance</div>
            <div className="text-sm text-gray-700">
              DevHub is built with Tauri for native performance and minimal resource usage
            </div>
          </div>
        </div>
      </div>
    </main>
  );
}

// Memoize the component to prevent unnecessary re-renders
export default memo(Dashboard);
