import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, WhatIfChange, WorkflowStatus } from '../types';
import BicepViewer from './BicepViewer';
import { ConfirmDialog } from './ConfirmDialog';
import DeploymentHistory from './DeploymentHistory';
import ResourceGrid from './ResourceGrid';
import { ActionCardGrid, ErrorDisplay, SuccessDisplay } from './ui';
import WhatIfViewer from './WhatIfViewer';

type Tab = 'actions' | 'bicep' | 'resources' | 'history';

function InfrastructurePanel() {
  const [activeTab, setActiveTab] = useState<Tab>('actions');
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [workflowStatus, setWorkflowStatus] = useState<WorkflowStatus | null>(null);
  const [whatIfChanges, setWhatIfChanges] = useState<WhatIfChange[]>([]);
  const [showDestroyConfirm, setShowDestroyConfirm] = useState(false);
  const deploymentMethod: 'github' | 'azure-cli' = 'azure-cli';
  const [repoRoot, setRepoRoot] = useState<string>('');
  const environment = 'dev';
  const [hasPreviewed, setHasPreviewed] = useState(false);
  const [showDeployConfirm, setShowDeployConfirm] = useState(false);
  const [showOutputPanel, setShowOutputPanel] = useState(false);

  const workflowFile = 'infrastructure-deploy-dev.yml';
  const repository = 'phoenixvc/Mystira.App';

  // Get repository root on mount
  useEffect(() => {
    const fetchRepoRoot = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
      } catch (error) {
        console.error('Failed to get repo root:', error);
      }
    };
    fetchRepoRoot();
  }, []);

  // Use stores
  const {
    resources,
    isLoading: resourcesLoading,
    error: resourcesError,
    fetchResources,
  } = useResourcesStore();

  const {
    deployments,
    isLoading: deploymentsLoading,
    error: deploymentsError,
    fetchDeployments,
  } = useDeploymentsStore();

  // Fetch resources when switching to resources tab
  useEffect(() => {
    if (activeTab === 'resources') {
      fetchResources();
    }
  }, [activeTab, fetchResources]);

  // Fetch deployments when switching to history tab
  useEffect(() => {
    if (activeTab === 'history') {
      fetchDeployments();
    }
  }, [activeTab, fetchDeployments]);

  const handleAction = async (action: 'validate' | 'preview' | 'deploy' | 'destroy') => {
    setLoading(true);
    setLastResponse(null);
    setShowOutputPanel(true);

    try {
      let response: CommandResponse;

      if (deploymentMethod === 'azure-cli' && repoRoot) {
        switch (action) {
          case 'validate':
            response = await invoke('azure_validate_infrastructure', {
              repoRoot,
              environment,
            });
            break;

          case 'preview':
            response = await invoke('azure_preview_infrastructure', {
              repoRoot,
              environment,
            });
            if (response.success && response.result) {
              const previewData = response.result as any;
              let parsedChanges: WhatIfChange[] = [];

              if (previewData.parsed && previewData.parsed.changes) {
                parsedChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
              } else if (previewData.preview) {
                parsedChanges = parseWhatIfOutput(previewData.preview);
              } else if (previewData.changes) {
                parsedChanges = previewData.changes;
              }

              if (parsedChanges.length > 0) {
                setWhatIfChanges(parsedChanges);
                setHasPreviewed(true);
              }
            }
            break;

          case 'deploy':
            if (!hasPreviewed || whatIfChanges.length === 0) {
              setLastResponse({
                success: false,
                error: 'Please run Preview first to see what will be deployed before deploying.',
              });
              setLoading(false);
              return;
            }

            const selectedResources = whatIfChanges
              .filter(c => c.selected !== false)
              .map(c => ({
                name: c.resourceName,
                type: c.resourceType,
                module: getModuleFromResourceType(c.resourceType),
              }));

            if (selectedResources.length === 0) {
              setLastResponse({
                success: false,
                error: 'Please select at least one resource to deploy.',
              });
              setLoading(false);
              return;
            }

            const selectedModules = new Set(selectedResources.map(r => r.module).filter(Boolean));
            if (selectedModules.has('appservice')) {
              if (!selectedModules.has('cosmos') || !selectedModules.has('storage')) {
                setLastResponse({
                  success: false,
                  error: 'App Service requires Cosmos DB and Storage Account to be selected.',
                });
                setLoading(false);
                return;
              }
            }

            setShowDeployConfirm(true);
            setLoading(false);
            return;

          case 'destroy':
            response = {
              success: false,
              error: 'Destroy action not available for direct Azure CLI deployment.',
            };
            break;

          default:
            throw new Error(`Unknown action: ${action}`);
        }
      } else {
        switch (action) {
          case 'validate':
            response = await invoke('infrastructure_validate', {
              workflowFile,
              repository,
            });
            break;

          case 'preview':
            response = await invoke('infrastructure_preview', {
              workflowFile,
              repository,
            });
            if (response.success) {
              setWhatIfChanges([
                {
                  resourceType: 'Microsoft.DocumentDB/databaseAccounts',
                  resourceName: 'dev-euw-cosmos-mystira',
                  changeType: 'modify',
                  changes: ['consistencyPolicy.defaultConsistencyLevel: BoundedStaleness → Session'],
                },
                {
                  resourceType: 'Microsoft.Storage/storageAccounts',
                  resourceName: 'deveuwstmystira',
                  changeType: 'noChange',
                },
              ]);
            }
            break;

          case 'deploy':
            const confirmDeploy = confirm(
              'Are you sure you want to deploy infrastructure?'
            );
            if (!confirmDeploy) {
              setLoading(false);
              return;
            }
            response = await invoke('infrastructure_deploy', {
              workflowFile,
              repository,
            });
            break;

          case 'destroy':
            response = await invoke('infrastructure_destroy', {
              workflowFile,
              repository,
              confirm: true,
            });
            break;

          default:
            throw new Error(`Unknown action: ${action}`);
        }
      }

      setLastResponse(response);

      if (response.success) {
        setTimeout(() => fetchWorkflowStatus(), 2000);
      }
    } catch (error) {
      setLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      setLoading(false);
    }
  };

  const getModuleFromResourceType = (resourceType: string): 'storage' | 'cosmos' | 'appservice' | null => {
    const normalized = resourceType.toLowerCase().trim();

    const storageTypes = [
      'microsoft.storage/storageaccounts',
      'microsoft.storage/storageaccounts/blobservices',
      'microsoft.storage/storageaccounts/blobservices/containers',
    ];

    const cosmosTypes = [
      'microsoft.documentdb/databaseaccounts',
      'microsoft.documentdb/databaseaccounts/sqldatabases',
      'microsoft.documentdb/databaseaccounts/sqldatabases/containers',
      'microsoft.documentdb/databaseaccounts/sqlroleassignments',
    ];

    const appServiceTypes = [
      'microsoft.web/sites',
      'microsoft.web/serverfarms',
      'microsoft.web/sites/config',
    ];

    if (storageTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'storage';
    }
    if (cosmosTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'cosmos';
    }
    if (appServiceTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'appservice';
    }

    if (normalized.includes('storage') && normalized.includes('account')) {
      return 'storage';
    }
    if (normalized.includes('documentdb') || normalized.includes('cosmos')) {
      return 'cosmos';
    }
    if (normalized.includes('web/sites') || normalized.includes('web/serverfarms')) {
      return 'appservice';
    }

    return null;
  };

  const fetchWorkflowStatus = async () => {
    try {
      const response: CommandResponse<WorkflowStatus> = await invoke('infrastructure_status', {
        workflowFile,
        repository,
      });

      if (response.success && response.result) {
        setWorkflowStatus(response.result);
      }
    } catch (error) {
      console.error('Failed to fetch workflow status:', error);
    }
  };

  const handleDestroyConfirm = async () => {
    setShowDestroyConfirm(false);
    await handleAction('destroy');
  };

  const handleDeployConfirm = async () => {
    setShowDeployConfirm(false);
    setLoading(true);

    try {
      const selectedResources = whatIfChanges
        .filter(c => c.selected !== false)
        .map(c => ({
          name: c.resourceName,
          type: c.resourceType,
          module: getModuleFromResourceType(c.resourceType),
        }));

      const selectedModules = new Set(selectedResources.map(r => r.module).filter(Boolean));
      const deployStorage = selectedModules.has('storage');
      const deployCosmos = selectedModules.has('cosmos');
      const deployAppService = selectedModules.has('appservice');

      const response = await invoke<CommandResponse>('azure_deploy_infrastructure', {
        repoRoot,
        environment,
        deployStorage,
        deployCosmos,
        deployAppService,
      });

      setLastResponse(response);

      if (response.success) {
        setTimeout(() => fetchWorkflowStatus(), 2000);
        setHasPreviewed(false);
        setWhatIfChanges([]);
      }
    } catch (error) {
      setLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      setLoading(false);
    }
  };

  const parseWhatIfOutput = (whatIfJson: string): WhatIfChange[] => {
    try {
      const parsed = typeof whatIfJson === 'string' ? JSON.parse(whatIfJson) : whatIfJson;
      const changes: WhatIfChange[] = [];

      let changesArray: any[] = [];

      if (Array.isArray(parsed)) {
        changesArray = parsed;
      } else if (parsed.changes && Array.isArray(parsed.changes)) {
        changesArray = parsed.changes;
      } else if (parsed.resourceChanges && Array.isArray(parsed.resourceChanges)) {
        changesArray = parsed.resourceChanges;
      } else if (parsed.properties?.changes && Array.isArray(parsed.properties.changes)) {
        changesArray = parsed.properties.changes;
      } else if (parsed.properties?.resourceChanges && Array.isArray(parsed.properties.resourceChanges)) {
        changesArray = parsed.properties.resourceChanges;
      }

      if (changesArray.length > 0) {
        changesArray.forEach((change: any) => {
          if (!change.resourceId && !change.targetResource?.id) {
            return;
          }

          const resourceId = change.resourceId || change.targetResource?.id || '';
          const resourceIdParts = resourceId.split('/');
          const resourceName = resourceIdParts[resourceIdParts.length - 1] || resourceId;

          let resourceType = change.resourceType ||
                            change.targetResource?.type ||
                            change.resource?.type ||
                            (resourceIdParts.length >= 8 ? `${resourceIdParts[6]}/${resourceIdParts[7]}` : 'Unknown');

          if (resourceType && resourceType !== 'Unknown') {
            resourceType = resourceType.replace(/^microsoft\./i, 'Microsoft.');
          }

          let changeType: 'create' | 'modify' | 'delete' | 'noChange' = 'noChange';
          const azChangeType = (change.changeType || change.action || '').toLowerCase().trim();

          if (azChangeType === 'create' || azChangeType === 'deploy' || azChangeType === 'new') {
            changeType = 'create';
          } else if (azChangeType === 'modify' || azChangeType === 'update' || azChangeType === 'change') {
            changeType = 'modify';
          } else if (azChangeType === 'delete' || azChangeType === 'remove' || azChangeType === 'destroy') {
            changeType = 'delete';
          } else if (azChangeType === 'nochange' || azChangeType === 'ignore' || azChangeType === 'no-op') {
            changeType = 'noChange';
          }

          const propertyChanges: string[] = [];
          const delta = change.delta || change.changes || change.properties;

          if (delta) {
            if (Array.isArray(delta)) {
              delta.forEach((d: any) => {
                if (d.path || d.property) {
                  const path = d.path || d.property;
                  propertyChanges.push(`${path}: ${d.before || d.oldValue || 'null'} → ${d.after || d.newValue || 'null'}`);
                }
              });
            } else if (typeof delta === 'object') {
              Object.keys(delta).forEach((key: string) => {
                const deltaValue = delta[key];
                if (Array.isArray(deltaValue)) {
                  deltaValue.forEach((d: any) => {
                    if (d.path || d.property) {
                      const path = d.path || d.property || key;
                      propertyChanges.push(`${path}: ${d.before || d.oldValue || 'null'} → ${d.after || d.newValue || 'null'}`);
                    }
                  });
                } else if (deltaValue && typeof deltaValue === 'object') {
                  propertyChanges.push(`${key}: ${JSON.stringify(deltaValue)}`);
                }
              });
            }
          }

          changes.push({
            resourceType: resourceType,
            resourceName: resourceName,
            changeType: changeType,
            changes: propertyChanges.length > 0 ? propertyChanges : undefined,
            selected: changeType !== 'noChange',
            resourceId: resourceId,
          });
        });
      }

      return changes;
    } catch (error) {
      console.error('Failed to parse what-if output:', error);
      return [];
    }
  };

  const actionButtons = [
    {
      id: 'validate',
      icon: '🔍',
      label: 'Validate',
      description: 'Check Bicep',
      onClick: () => handleAction('validate'),
      disabled: loading,
      loading: loading,
      variant: 'primary' as const,
    },
    {
      id: 'preview',
      icon: '👁️',
      label: 'Preview',
      description: 'What-if',
      onClick: () => handleAction('preview'),
      disabled: loading,
      loading: loading,
      variant: 'warning' as const,
    },
    {
      id: 'deploy',
      icon: '🚀',
      label: 'Deploy',
      description: hasPreviewed ? `${whatIfChanges.filter(c => c.selected !== false).length} selected` : 'Preview first',
      onClick: () => handleAction('deploy'),
      disabled: loading || !hasPreviewed,
      loading: loading,
      variant: 'success' as const,
    },
    {
      id: 'destroy',
      icon: '🗑️',
      label: 'Destroy',
      description: 'Delete all',
      onClick: () => setShowDestroyConfirm(true),
      disabled: loading,
      loading: loading,
      variant: 'danger' as const,
    },
  ];

  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-900">
      <ConfirmDialog
        isOpen={showDestroyConfirm}
        title="⚠️ Destroy Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirm}
        onCancel={() => setShowDestroyConfirm(false)}
      />
      <ConfirmDialog
        isOpen={showDeployConfirm}
        title="🚀 Deploy Selected Resources"
        message={`You are about to deploy ${whatIfChanges.filter(c => c.selected !== false).length} selected resource(s) to ${environment} environment.`}
        confirmText="Deploy Selected Resources"
        cancelText="Cancel"
        confirmButtonClass="bg-green-600 hover:bg-green-700"
        onConfirm={handleDeployConfirm}
        onCancel={() => setShowDeployConfirm(false)}
      />

      {/* Header */}
      <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 flex items-center justify-between">
        <div>
          <h2 className="text-base font-bold text-gray-900 dark:text-white">Infrastructure Control</h2>
          <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
            <span>📁 {workflowFile}</span>
            <span>🌍 {environment}</span>
          </div>
        </div>
        <ActionCardGrid actions={actionButtons} columns={4} />
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        <nav className="flex space-x-1 px-4">
          {[
            { id: 'actions', icon: '⚡', label: 'Actions' },
            { id: 'bicep', icon: '📄', label: 'Bicep' },
            { id: 'resources', icon: '☁️', label: 'Resources' },
            { id: 'history', icon: '📜', label: 'History' },
          ].map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as Tab)}
              className={`px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
                activeTab === tab.id
                  ? 'border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
              }`}
            >
              {tab.icon} {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 flex min-h-0">
        {/* Tab Content */}
        <div className={`flex-1 overflow-auto p-4 ${showOutputPanel ? '' : ''}`}>
          {activeTab === 'actions' && (
            <div className="space-y-4">
              {loading && (
                <div className="flex items-center gap-2 text-blue-600 text-xs bg-blue-50 dark:bg-blue-900/30 px-3 py-2 rounded">
                  <span className="animate-spin">⟳</span>
                  <span>Running command...</span>
                </div>
              )}
              {whatIfChanges.length > 0 && (
                <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="px-3 py-2 border-b border-gray-200 dark:border-gray-700">
                    <h3 className="text-xs font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                      🔍 Preview Changes
                      <span className="text-[10px] px-1.5 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full">
                        {whatIfChanges.filter(c => c.selected !== false).length} selected
                      </span>
                    </h3>
                  </div>
                  <div className="p-2">
                    <WhatIfViewer
                      changes={whatIfChanges}
                      loading={loading}
                      showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
                      onSelectionChange={(updated) => setWhatIfChanges(updated)}
                      compact
                    />
                  </div>
                </div>
              )}
              {!loading && whatIfChanges.length === 0 && (
                <div className="text-center text-gray-500 dark:text-gray-400 py-8 text-sm">
                  <p className="mb-2">Ready to manage infrastructure</p>
                  <p className="text-xs">Click <strong>Validate</strong> to check templates, or <strong>Preview</strong> to see changes</p>
                </div>
              )}
            </div>
          )}

          {activeTab === 'bicep' && <BicepViewer />}

          {activeTab === 'resources' && (
            <div>
              {resourcesLoading && (
                <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-6 text-center">
                  <div className="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 mb-2"></div>
                  <p className="text-blue-800 dark:text-blue-200 text-sm">Loading resources...</p>
                </div>
              )}
              {resourcesError && (
                <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-4">
                  <h3 className="text-sm font-semibold text-red-900 dark:text-red-300 mb-1">❌ Failed to Load</h3>
                  <p className="text-red-800 dark:text-red-200 text-xs mb-2">{resourcesError}</p>
                  <button
                    onClick={() => fetchResources(true)}
                    className="px-3 py-1 bg-red-600 text-white rounded text-xs hover:bg-red-700"
                  >
                    Retry
                  </button>
                </div>
              )}
              {!resourcesLoading && !resourcesError && (
                <ResourceGrid resources={resources} onRefresh={() => fetchResources(true)} compact />
              )}
            </div>
          )}

          {activeTab === 'history' && (
            <div>
              {deploymentsLoading && (
                <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-6 text-center">
                  <div className="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 mb-2"></div>
                  <p className="text-blue-800 dark:text-blue-200 text-sm">Loading history...</p>
                </div>
              )}
              {deploymentsError && (
                <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-4">
                  <h3 className="text-sm font-semibold text-red-900 dark:text-red-300 mb-1">❌ Failed to Load</h3>
                  <p className="text-red-800 dark:text-red-200 text-xs mb-2">{deploymentsError}</p>
                  <button
                    onClick={() => fetchDeployments(true)}
                    className="px-3 py-1 bg-red-600 text-white rounded text-xs hover:bg-red-700"
                  >
                    Retry
                  </button>
                </div>
              )}
              {!deploymentsLoading && !deploymentsError && (
                <DeploymentHistory events={deployments} />
              )}
            </div>
          )}
        </div>

        {/* Inline Output Panel (collapsible) */}
        {showOutputPanel && (
          <div className="w-80 border-l border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 flex flex-col">
            <div className="flex items-center justify-between px-3 py-2 border-b border-gray-200 dark:border-gray-700">
              <span className="text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase">Output</span>
              <button
                onClick={() => setShowOutputPanel(false)}
                className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
              >
                ✕
              </button>
            </div>
            <div className="flex-1 overflow-auto p-3 text-xs">
              {loading && (
                <div className="flex items-center gap-2 text-blue-600">
                  <span className="animate-spin">⟳</span>
                  <span>Executing...</span>
                </div>
              )}
              {lastResponse && (
                lastResponse.success ? (
                  <SuccessDisplay message={lastResponse.message || 'Success'} details={lastResponse.result as Record<string, unknown> | null} />
                ) : (
                  <ErrorDisplay error={lastResponse.error || 'Error'} details={lastResponse.result as Record<string, unknown> | null} />
                )
              )}
              {!loading && !lastResponse && (
                <div className="text-gray-500">No output yet.</div>
              )}

              {/* Workflow Status */}
              {workflowStatus && (
                <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-600">
                  <div className="text-[10px] font-semibold text-gray-500 uppercase mb-2">Workflow</div>
                  <div className="grid grid-cols-2 gap-2 text-xs">
                    <div>
                      <div className="text-gray-400">Status</div>
                      <div className="font-medium text-gray-900 dark:text-white">{workflowStatus.status || 'Unknown'}</div>
                    </div>
                    <div>
                      <div className="text-gray-400">Conclusion</div>
                      <div className="font-medium text-gray-900 dark:text-white">{workflowStatus.conclusion || 'N/A'}</div>
                    </div>
                  </div>
                  <div className="flex gap-2 mt-2">
                    {workflowStatus.htmlUrl && (
                      <a
                        href={workflowStatus.htmlUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="px-2 py-1 bg-blue-600 text-white rounded text-[10px] hover:bg-blue-700"
                      >
                        GitHub →
                      </a>
                    )}
                    <button
                      onClick={fetchWorkflowStatus}
                      className="px-2 py-1 bg-gray-600 text-white rounded text-[10px] hover:bg-gray-700"
                    >
                      Refresh
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Bottom bar with output toggle */}
      {!showOutputPanel && lastResponse && (
        <button
          onClick={() => setShowOutputPanel(true)}
          className={`px-4 py-2 text-xs border-t border-gray-200 dark:border-gray-700 flex items-center gap-2 ${
            lastResponse.success
              ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300'
              : 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300'
          }`}
        >
          <span>{lastResponse.success ? '✓' : '✕'}</span>
          <span>
            {lastResponse.success
              ? (lastResponse.message || 'Operation completed')
              : (lastResponse.error || 'Operation failed')}
          </span>
          <span className="ml-auto text-gray-400">Click to expand</span>
        </button>
      )}
    </div>
  );
}

export default InfrastructurePanel;
