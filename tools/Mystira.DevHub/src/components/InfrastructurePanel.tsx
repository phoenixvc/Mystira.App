import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, WhatIfChange, WorkflowStatus } from '../types';
import { ActionCardGrid } from './ActionToolbar';
import BicepViewer from './BicepViewer';
import { ConfirmDialog } from './ConfirmDialog';
import DeploymentHistory from './DeploymentHistory';
import { DockableLayout, type DockablePanelConfig, TabbedPanel } from './DockableInfrastructurePanel';
import ResourceGrid from './ResourceGrid';
import { ErrorDisplay, SuccessDisplay } from './ResizableOutputPanel';
import WhatIfViewer from './WhatIfViewer';

type Tab = 'actions' | 'bicep' | 'resources' | 'history';

function InfrastructurePanel() {
  const [activeTab, setActiveTab] = useState<Tab>('actions');
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [workflowStatus, setWorkflowStatus] = useState<WorkflowStatus | null>(null);
  const [whatIfChanges, setWhatIfChanges] = useState<WhatIfChange[]>([]);
  const [showDestroyConfirm, setShowDestroyConfirm] = useState(false);
  const deploymentMethod: 'github' | 'azure-cli' = 'azure-cli'; // Always use Azure CLI for now
  const [repoRoot, setRepoRoot] = useState<string>('');
  const environment = 'dev'; // Always use dev for now
  const [hasPreviewed, setHasPreviewed] = useState(false);
  const [showDeployConfirm, setShowDeployConfirm] = useState(false);

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

  // Use stores instead of local state
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

    try {
      let response: CommandResponse;

      if (deploymentMethod === 'azure-cli' && repoRoot) {
        // Use direct Azure CLI deployment
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
              // Parse what-if output
              const previewData = response.result as any;
              let parsedChanges: WhatIfChange[] = [];
              
              if (previewData.parsed && previewData.parsed.changes) {
                // Use parsed JSON if available
                parsedChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
              } else if (previewData.preview) {
                // Try to parse from preview text/JSON
                parsedChanges = parseWhatIfOutput(previewData.preview);
              } else if (previewData.changes) {
                // Already parsed
                parsedChanges = previewData.changes;
              }
              
              if (parsedChanges.length > 0) {
                setWhatIfChanges(parsedChanges);
                setHasPreviewed(true);
              }
            }
            break;

          case 'deploy':
            // Require preview first
            if (!hasPreviewed || whatIfChanges.length === 0) {
              setLastResponse({
                success: false,
                error: 'Please run Preview first to see what will be deployed before deploying.',
              });
              setLoading(false);
              return;
            }
            
            // Get selected resources with their types
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
            
            // Validate dependencies: App Service requires Cosmos and Storage
            const selectedModules = new Set(selectedResources.map(r => r.module).filter(Boolean));
            if (selectedModules.has('appservice')) {
              if (!selectedModules.has('cosmos') || !selectedModules.has('storage')) {
                setLastResponse({
                  success: false,
                  error: 'App Service requires Cosmos DB and Storage Account to be selected. Please select all dependencies.',
                });
                setLoading(false);
                return;
              }
            }
            
            // Show confirmation dialog
            setShowDeployConfirm(true);
            setLoading(false);
            return;

          case 'destroy':
            // Destroy not implemented for direct Azure CLI yet
            response = {
              success: false,
              error: 'Destroy action not available for direct Azure CLI deployment. Use GitHub Actions workflow instead.',
            };
            break;

          default:
            throw new Error(`Unknown action: ${action}`);
        }
      } else {
        // Use GitHub Actions workflow
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
            // Mock what-if changes for demonstration
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
              'Are you sure you want to deploy infrastructure? This will create or update Azure resources.'
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
            // This should not be reached directly - destroy should go through confirmation dialog
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

      // If successful, fetch the workflow status
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

  // Map Azure resource type to our module identifier (exact matching for reliability)
  const getModuleFromResourceType = (resourceType: string): 'storage' | 'cosmos' | 'appservice' | null => {
    const normalized = resourceType.toLowerCase().trim();
    
    // Exact type matching for reliability (Azure resource types are standardized)
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
    
    // Check exact matches first
    if (storageTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'storage';
    }
    if (cosmosTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'cosmos';
    }
    if (appServiceTypes.some(type => normalized === type || normalized.startsWith(type + '/'))) {
      return 'appservice';
    }
    
    // Fallback to substring matching for edge cases
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
      // Get selected resources with module mapping
      const selectedResources = whatIfChanges
        .filter(c => c.selected !== false)
        .map(c => ({
          name: c.resourceName,
          type: c.resourceType,
          module: getModuleFromResourceType(c.resourceType),
        }));
      
      // Extract module flags
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
        setHasPreviewed(false); // Reset preview after successful deployment
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

  // Parse Azure what-if JSON output (handles multiple Azure output formats)
  const parseWhatIfOutput = (whatIfJson: string): WhatIfChange[] => {
    try {
      const parsed = typeof whatIfJson === 'string' ? JSON.parse(whatIfJson) : whatIfJson;
      const changes: WhatIfChange[] = [];
      
      // Azure what-if output can have different structures:
      // Format 1: { status, changes: [...] }
      // Format 2: { status, resourceChanges: [...] }
      // Format 3: Direct array of changes
      // Format 4: { properties: { changes: [...] } }
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
            return; // Skip invalid entries
          }
          
          const resourceId = change.resourceId || change.targetResource?.id || '';
          const resourceIdParts = resourceId.split('/');
          const resourceName = resourceIdParts[resourceIdParts.length - 1] || resourceId;
          
          // Extract resource type from multiple possible locations
          let resourceType = change.resourceType || 
                            change.targetResource?.type ||
                            change.resource?.type ||
                            (resourceIdParts.length >= 8 ? `${resourceIdParts[6]}/${resourceIdParts[7]}` : 'Unknown');
          
          // Normalize resource type format
          if (resourceType && resourceType !== 'Unknown') {
            resourceType = resourceType.replace(/^microsoft\./i, 'Microsoft.');
          }
          
          // Map Azure change types to our types
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
          
          // Extract property changes from delta (handles multiple delta formats)
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
                  // Nested delta structure
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
            selected: changeType !== 'noChange', // Auto-select resources that will change
            resourceId: resourceId,
          });
        });
      }
      
      return changes;
    } catch (error) {
      console.error('Failed to parse what-if output:', error);
      console.error('Raw output:', whatIfJson);
      // Fallback: return empty array
      return [];
    }
  };

  // Prepare action buttons config
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

  // Build output content for right panel with tabs
  const outputPanelContent = (
    <TabbedPanel
      storageKey="infraOutputTabs"
      tabs={[
        {
          id: 'output',
          label: 'Output',
          icon: '📋',
          badge: lastResponse ? (lastResponse.success ? '✓' : '✕') : undefined,
          content: (
            <div className="h-full overflow-auto">
              {loading && (
                <div className="p-3 flex items-center gap-2 text-blue-600 dark:text-blue-400 text-xs">
                  <span className="animate-spin">⟳</span>
                  <span>Executing command...</span>
                </div>
              )}
              {lastResponse && (
                lastResponse.success ? (
                  <SuccessDisplay message={lastResponse.message || 'Operation completed successfully'} details={lastResponse.result as Record<string, unknown> | null} />
                ) : (
                  <ErrorDisplay error={lastResponse.error || 'An error occurred'} details={lastResponse.result as Record<string, unknown> | null} />
                )
              )}
              {!loading && !lastResponse && (
                <div className="p-3 text-xs text-gray-500 dark:text-gray-400">
                  No output yet. Run an action to see results here.
                </div>
              )}
            </div>
          ),
        },
        {
          id: 'whatif',
          label: 'Preview',
          icon: '🔍',
          badge: whatIfChanges.length > 0 ? whatIfChanges.filter(c => c.selected !== false).length : undefined,
          content: (
            <div className="h-full overflow-auto">
              {whatIfChanges.length > 0 ? (
                <div className="p-2">
                  <WhatIfViewer
                    changes={whatIfChanges}
                    loading={loading && activeTab === 'actions'}
                    showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
                    onSelectionChange={(updated) => setWhatIfChanges(updated)}
                    compact
                  />
                </div>
              ) : (
                <div className="p-3 text-xs text-gray-500 dark:text-gray-400">
                  No preview. Run Preview to see changes.
                </div>
              )}
            </div>
          ),
        },
        {
          id: 'workflow',
          label: 'Workflow',
          icon: '⚙️',
          content: (
            <div className="h-full overflow-auto p-3 text-xs">
              {workflowStatus ? (
                <div className="space-y-2">
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <div className="text-gray-500 dark:text-gray-400">Status</div>
                      <div className="font-semibold text-gray-900 dark:text-white">{workflowStatus.status || 'Unknown'}</div>
                    </div>
                    <div>
                      <div className="text-gray-500 dark:text-gray-400">Conclusion</div>
                      <div className="font-semibold text-gray-900 dark:text-white">{workflowStatus.conclusion || 'N/A'}</div>
                    </div>
                  </div>
                  <div className="flex gap-2 mt-2">
                    {workflowStatus.htmlUrl && (
                      <a
                        href={workflowStatus.htmlUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="px-2 py-1 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 transition-colors"
                      >
                        GitHub →
                      </a>
                    )}
                    <button
                      onClick={fetchWorkflowStatus}
                      className="px-2 py-1 bg-gray-600 text-white rounded text-xs hover:bg-gray-700 transition-colors"
                    >
                      Refresh
                    </button>
                  </div>
                </div>
              ) : (
                <div className="text-gray-500 dark:text-gray-400">
                  No workflow status available.
                </div>
              )}
            </div>
          ),
        },
      ]}
    />
  );

  // Main content panel
  const mainContent = (
    <div className="h-full flex flex-col overflow-hidden">
      {/* Header - Compact */}
      <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between bg-gray-50 dark:bg-gray-800/50">
        <div>
          <h2 className="text-lg font-bold text-gray-900 dark:text-white">
            Infrastructure Control
          </h2>
          <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            <span>📁 {workflowFile}</span>
            <span>🌍 {environment}</span>
          </div>
        </div>
        <ActionCardGrid actions={actionButtons} columns={4} />
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 dark:border-gray-700">
        <nav className="flex space-x-1 px-4">
          <button
            onClick={() => setActiveTab('actions')}
            className={`px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
              activeTab === 'actions'
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
            }`}
          >
            ⚡ Actions
          </button>
          <button
            onClick={() => setActiveTab('bicep')}
            className={`px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
              activeTab === 'bicep'
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
            }`}
          >
            📄 Bicep
          </button>
          <button
            onClick={() => setActiveTab('resources')}
            className={`px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
              activeTab === 'resources'
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
            }`}
          >
            ☁️ Resources
          </button>
          <button
            onClick={() => setActiveTab('history')}
            className={`px-3 py-2 text-xs font-medium transition-colors border-b-2 ${
              activeTab === 'history'
                ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300'
            }`}
          >
            📜 History
          </button>
        </nav>
      </div>

      {/* Tab Content */}
      <div className="flex-1 overflow-auto p-4">
        {activeTab === 'actions' && (
          <div className="space-y-4">
            {loading && (
              <div className="flex items-center gap-2 text-blue-600 dark:text-blue-400 text-xs bg-blue-50 dark:bg-blue-900/30 px-3 py-2 rounded">
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
                <div className="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 dark:border-blue-400 mb-2"></div>
                <p className="text-blue-800 dark:text-blue-200 text-sm">Loading resources...</p>
              </div>
            )}
            {resourcesError && (
              <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-4">
                <h3 className="text-sm font-semibold text-red-900 dark:text-red-300 mb-1">❌ Failed to Load</h3>
                <p className="text-red-800 dark:text-red-200 text-xs mb-2">{resourcesError}</p>
                <button
                  onClick={() => fetchResources(true)}
                  className="px-3 py-1 bg-red-600 dark:bg-red-500 text-white rounded text-xs hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
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
                <div className="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600 dark:border-blue-400 mb-2"></div>
                <p className="text-blue-800 dark:text-blue-200 text-sm">Loading history...</p>
              </div>
            )}
            {deploymentsError && (
              <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-4">
                <h3 className="text-sm font-semibold text-red-900 dark:text-red-300 mb-1">❌ Failed to Load</h3>
                <p className="text-red-800 dark:text-red-200 text-xs mb-2">{deploymentsError}</p>
                <button
                  onClick={() => fetchDeployments(true)}
                  className="px-3 py-1 bg-red-600 dark:bg-red-500 text-white rounded-lg text-xs hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
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
    </div>
  );

  // Define dockable panels configuration
  const dockablePanels: DockablePanelConfig[] = [
    {
      id: 'main-content',
      title: 'Infrastructure',
      content: mainContent,
      defaultPosition: 'center',
    },
    {
      id: 'output-panel',
      title: 'Output',
      icon: '📋',
      content: outputPanelContent,
      defaultPosition: 'right',
      defaultSize: { width: 350 },
      minSize: { width: 250 },
      collapsible: true,
      defaultCollapsed: false,
    },
  ];

  return (
    <div className="h-[calc(100vh-120px)]">
      <ConfirmDialog
        isOpen={showDestroyConfirm}
        title="⚠️ Destroy Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirm}
        onCancel={() => setShowDestroyConfirm(false)}
      />
      <ConfirmDialog
        isOpen={showDeployConfirm}
        title="🚀 Deploy Selected Resources"
        message={`You are about to deploy ${whatIfChanges.filter(c => c.selected !== false).length} selected resource(s) to ${environment} environment. This will create or update Azure resources in resource group dev-euw-rg-mystira-app.`}
        confirmText="Deploy Selected Resources"
        cancelText="Cancel"
        confirmButtonClass="bg-green-600 hover:bg-green-700 dark:bg-green-500 dark:hover:bg-green-600"
        onConfirm={handleDeployConfirm}
        onCancel={() => setShowDeployConfirm(false)}
      />

      <DockableLayout
        panels={dockablePanels}
        storageKey="infraPanelLayout"
      />
    </div>
  );
}

export default InfrastructurePanel;
