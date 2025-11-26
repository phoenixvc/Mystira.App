import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, WhatIfChange, WorkflowStatus } from '../types';
import BicepViewer from './BicepViewer';
import { ConfirmDialog } from './ConfirmDialog';
import DeploymentHistory from './DeploymentHistory';
import { DestroyButton } from './DestroyButton';
import ResourceGrid from './ResourceGrid';
import WhatIfViewer from './WhatIfViewer';
import { formatTimeSince } from './services/utils/serviceUtils';

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

  const workflowFile = '.start-infrastructure-deploy-dev.yml';
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

  const [isBuildingCli, setIsBuildingCli] = useState(false);
  const [cliBuildTime, setCliBuildTime] = useState<number | null>(null);
  const [cliBuildLogs, setCliBuildLogs] = useState<string[]>([]);
  const [showCliBuildLogs, setShowCliBuildLogs] = useState(false);
  const cliLogsEndRef = useRef<HTMLDivElement>(null);

  // Fetch workflow status on mount to show last build time
  useEffect(() => {
    fetchWorkflowStatus();
  }, []);

  // Fetch CLI build time on mount and after building
  useEffect(() => {
    const fetchCliBuildTime = async () => {
      try {
        const buildTime = await invoke<number | null>('get_cli_build_time');
        setCliBuildTime(buildTime);
      } catch (error) {
        console.error('Failed to get CLI build time:', error);
        setCliBuildTime(null);
      }
    };
    fetchCliBuildTime();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isBuildingCli]);

  // Auto-scroll CLI logs to bottom
  useEffect(() => {
    if (cliLogsEndRef.current && (isBuildingCli || cliBuildLogs.length > 0)) {
      cliLogsEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [cliBuildLogs, isBuildingCli]);

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

  // Fetch workflow status on mount to show last build time
  useEffect(() => {
    const loadWorkflowStatus = async () => {
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
    loadWorkflowStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
      const errorMessage = error instanceof Error ? error.message : String(error);
      const isCliNotFound = errorMessage.includes('program not found') || 
                            errorMessage.includes('Could not find Mystira.DevHub.CLI') ||
                            errorMessage.includes('Failed to spawn process');
      
      setLastResponse({
        success: false,
        error: isCliNotFound
          ? `❌ Program Not Found\n\n${errorMessage}\n\nPlease build the CLI executable first:\n1. Open a terminal\n2. Navigate to: tools/Mystira.DevHub.CLI\n3. Run: dotnet build`
          : errorMessage,
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

  return (
    <div className="p-8">
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
      <div className="max-w-7xl mx-auto">
        <div className="mb-8">
          <div className="flex items-center justify-between mb-2">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                Infrastructure Control Panel
              </h2>
              <p className="text-gray-600 dark:text-gray-400">
                Manage Bicep infrastructure deployments via GitHub Actions
              </p>
            </div>
            {/* Last Build Time Indicators */}
            <div className="flex flex-col items-end gap-2">
              {workflowStatus?.updatedAt && (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last Workflow Build</div>
                  <div className="px-3 py-1.5 rounded-lg bg-blue-900/20 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-mono font-semibold text-sm" 
                       title={`Last workflow build: ${new Date(workflowStatus.updatedAt).toLocaleString()}`}>
                    {formatTimeSince(new Date(workflowStatus.updatedAt).getTime()) || 'Unknown'}
                  </div>
                </div>
              )}
              {cliBuildTime ? (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last CLI Build</div>
                  <div className="px-3 py-1.5 rounded-lg bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400 font-mono font-semibold text-sm" 
                       title={`Last CLI build: ${new Date(cliBuildTime).toLocaleString()}`}>
                    {formatTimeSince(cliBuildTime) || 'Unknown'}
                  </div>
                </div>
              ) : (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">CLI Status</div>
                  <div className="flex items-center gap-2">
                    <div className="px-3 py-1.5 rounded-lg bg-red-900/20 dark:bg-red-900/30 text-red-600 dark:text-red-400 font-mono font-semibold text-sm">
                      Not Built
                    </div>
                    <button
                      onClick={async () => {
                        setIsBuildingCli(true);
                        setShowCliBuildLogs(true);
                        setCliBuildLogs([]);
                        try {
                          const response = await invoke<CommandResponse>('build_cli');
                          // Parse output from result
                          if (response.result && typeof response.result === 'object' && 'output' in response.result) {
                            const output = (response.result as any).output as string;
                            const lines = output.split('\n').filter(line => line.trim().length > 0);
                            setCliBuildLogs(lines);
                          }
                          if (response.success) {
                            // Get build time from response if available, otherwise fetch it
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                // Fallback: try to get it after a short delay
                                setTimeout(async () => {
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    setCliBuildTime(buildTime);
                                  } catch (error) {
                                    console.error('Failed to get CLI build time:', error);
                                  }
                                }, 1000);
                              }
                            } else {
                              // Fallback: fetch build time
                              setTimeout(async () => {
                                try {
                                  const buildTime = await invoke<number | null>('get_cli_build_time');
                                  setCliBuildTime(buildTime);
                                } catch (error) {
                                  console.error('Failed to get CLI build time:', error);
                                }
                              }, 1000);
                            }
                          } else {
                            // Keep logs visible on failure
                            console.error('Build failed:', response.error);
                          }
                        } catch (error) {
                          setCliBuildLogs([`Error: ${error}`]);
                          console.error('Failed to build CLI:', error);
                        } finally {
                          setIsBuildingCli(false);
                        }
                      }}
                      disabled={isBuildingCli}
                      className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5"
                      title="Build the CLI executable"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                          Building...
                        </>
                      ) : (
                        <>
                          🔨 Build CLI
                        </>
                      )}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* CLI Build Logs Viewer */}
        {(showCliBuildLogs && (isBuildingCli || cliBuildLogs.length > 0)) && (
          <div className="mb-6 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
            <div className="bg-gray-50 dark:bg-gray-800 px-4 py-2 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2">
                <h3 className="font-semibold text-gray-900 dark:text-white">CLI Build Logs</h3>
                {isBuildingCli && (
                  <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></span>
                )}
              </div>
              <button
                onClick={() => setShowCliBuildLogs(false)}
                className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
                title="Close logs"
              >
                ✕
              </button>
            </div>
            <div className="bg-gray-900 text-green-400 font-mono text-sm p-4 max-h-96 overflow-y-auto">
              {cliBuildLogs.length === 0 ? (
                <div className="text-gray-500">Waiting for build output...</div>
              ) : (
                <>
                  {cliBuildLogs.map((line, index) => (
                    <div key={index} className="whitespace-pre-wrap">
                      {line}
                    </div>
                  ))}
                  <div ref={cliLogsEndRef} />
                </>
              )}
            </div>
          </div>
        )}

        {/* Tabs */}
        <div className="mb-6">
          <nav className="flex space-x-1 border-b border-gray-200 dark:border-gray-700">
            <button
              onClick={() => setActiveTab('actions')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'actions'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              ⚡ Actions
            </button>
            <button
              onClick={() => setActiveTab('bicep')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'bicep'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              📄 Bicep Templates
            </button>
            <button
              onClick={() => setActiveTab('resources')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'resources'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              ☁️ Azure Resources
            </button>
            <button
              onClick={() => setActiveTab('history')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'history'
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
              }`}
            >
              📜 History
            </button>
          </nav>
        </div>

        {/* Tab Content: Actions */}
        {activeTab === 'actions' && (
          <div>
            {/* Action Buttons */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
              <button
                onClick={() => handleAction('validate')}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white dark:bg-gray-800 border-2 border-blue-200 dark:border-blue-800 rounded-lg hover:border-blue-400 dark:hover:border-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">🔍</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">Validate</div>
                <div className="text-sm text-gray-500 dark:text-gray-400 text-center mt-1">
                  Check Bicep templates
                </div>
              </button>

              <button
                onClick={() => handleAction('preview')}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white dark:bg-gray-800 border-2 border-yellow-200 dark:border-yellow-800 rounded-lg hover:border-yellow-400 dark:hover:border-yellow-600 hover:bg-yellow-50 dark:hover:bg-yellow-900/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">👁️</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">Preview</div>
                <div className="text-sm text-gray-500 dark:text-gray-400 text-center mt-1">
                  What-if analysis
                </div>
              </button>

              <button
                onClick={() => handleAction('deploy')}
                disabled={loading || !hasPreviewed}
                className="flex flex-col items-center p-6 bg-white dark:bg-gray-800 border-2 border-green-200 dark:border-green-800 rounded-lg hover:border-green-400 dark:hover:border-green-600 hover:bg-green-50 dark:hover:bg-green-900/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                title={!hasPreviewed ? "Please run Preview first" : ""}
              >
                <div className="text-4xl mb-2">🚀</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">Deploy</div>
                <div className="text-sm text-gray-500 dark:text-gray-400 text-center mt-1">
                  {hasPreviewed ? `Deploy selected (${whatIfChanges.filter(c => c.selected !== false).length})` : "Preview first"}
                </div>
              </button>

              <DestroyButton
                onClick={() => setShowDestroyConfirm(true)}
                disabled={loading}
                loading={loading}
              />
            </div>

            {/* Loading State */}
            {loading && (
              <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-4 mb-8">
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 dark:border-blue-400 mr-3"></div>
                  <span className="text-blue-800 dark:text-blue-200">Executing command...</span>
                </div>
              </div>
            )}

            {/* Response Display */}
            {lastResponse && (
              <div
                className={`rounded-lg p-6 mb-8 ${
                  lastResponse.success
                    ? 'bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800'
                    : 'bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800'
                }`}
              >
                <h3
                  className={`text-lg font-semibold mb-2 ${
                    lastResponse.success ? 'text-green-900 dark:text-green-300' : 'text-red-900 dark:text-red-300'
                  }`}
                >
                  {lastResponse.success ? '✅ Success' : '❌ Error'}
                </h3>

                {lastResponse.message && (
                  <p
                    className={`mb-3 ${
                      lastResponse.success ? 'text-green-800 dark:text-green-200' : 'text-red-800 dark:text-red-200'
                    }`}
                  >
                    {lastResponse.message}
                  </p>
                )}

                {lastResponse.error && (
                  <pre className="bg-red-100 dark:bg-red-900/50 p-3 rounded text-sm text-red-900 dark:text-red-200 overflow-auto">
                    {lastResponse.error}
                  </pre>
                )}

                {lastResponse.result !== undefined && lastResponse.result !== null && (
                  <details className="mt-3">
                    <summary
                      className={`cursor-pointer font-medium ${
                        lastResponse.success ? 'text-green-700 dark:text-green-300' : 'text-red-700 dark:text-red-300'
                      }`}
                    >
                      View Details
                    </summary>
                    <pre
                      className={`mt-2 p-3 rounded text-sm overflow-auto ${
                        lastResponse.success
                          ? 'bg-green-100 dark:bg-green-900/50 text-green-900 dark:text-green-200'
                          : 'bg-red-100 dark:bg-red-900/50 text-red-900 dark:text-red-200'
                      }`}
                    >
                      {JSON.stringify(lastResponse.result, null, 2) || 'No details available'}
                    </pre>
                  </details>
                )}
              </div>
            )}

            {/* What-If Viewer */}
            {whatIfChanges.length > 0 && (
              <div className="mb-8">
                <WhatIfViewer 
                  changes={whatIfChanges} 
                  loading={loading && activeTab === 'actions'}
                  showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
                  onSelectionChange={(updated) => setWhatIfChanges(updated)}
                />
              </div>
            )}

            {/* Workflow Status */}
            {workflowStatus && (
              <div className="bg-white border border-gray-200 rounded-lg p-6 mb-8">
                <h3 className="text-xl font-semibold text-gray-900 mb-4">
                  Workflow Status
                </h3>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                  <div>
                    <div className="text-sm text-gray-500">Status</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.status || 'Unknown'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Conclusion</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.conclusion || 'N/A'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Workflow</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.workflowName || 'N/A'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Updated</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.updatedAt
                        ? new Date(workflowStatus.updatedAt).toLocaleTimeString()
                        : 'N/A'}
                    </div>
                  </div>
                </div>

                {workflowStatus.htmlUrl && (
                  <a
                    href={workflowStatus.htmlUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-block px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    View in GitHub →
                  </a>
                )}

                <button
                  onClick={fetchWorkflowStatus}
                  className="ml-3 inline-block px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition-colors"
                >
                  Refresh Status
                </button>
              </div>
            )}

            {/* Info Box */}
            <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-300 mb-2">ℹ️ Information</h4>
              <ul className="text-sm text-blue-800 dark:text-blue-200 space-y-1 list-disc list-inside">
                <li>Workflow: {workflowFile}</li>
                <li>Repository: {repository}</li>
                <li>All actions trigger GitHub Actions workflows</li>
                <li>Requires GitHub CLI to be authenticated</li>
              </ul>
            </div>
          </div>
        )}

        {/* Tab Content: Bicep Viewer */}
        {activeTab === 'bicep' && (
          <div>
            <BicepViewer />
          </div>
        )}

        {/* Tab Content: Azure Resources */}
        {activeTab === 'resources' && (
          <div>
            {resourcesLoading && (
              <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-8 text-center">
                <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:border-blue-400 mb-3"></div>
                <p className="text-blue-800 dark:text-blue-200">Loading Azure resources...</p>
              </div>
            )}

            {resourcesError && (
              <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-4">
                <h3 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">❌ Failed to Load Resources</h3>
                <p className="text-red-800 dark:text-red-200 mb-3 whitespace-pre-wrap">{resourcesError}</p>
                <div className="flex gap-3">
                  <button
                    onClick={() => fetchResources(true)}
                    className="px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
                  >
                    Retry
                  </button>
                  {(resourcesError.includes('Could not find Mystira.DevHub.CLI') ||
                    resourcesError.includes('Program not found') ||
                    resourcesError.includes('Failed to spawn process')) && (
                    <button
                      onClick={async () => {
                        setIsBuildingCli(true);
                        setShowCliBuildLogs(true);
                        setCliBuildLogs([]);
                        try {
                          const response = await invoke<CommandResponse>('build_cli');
                          // Parse output from result
                          if (response.result && typeof response.result === 'object' && 'output' in response.result) {
                            const output = (response.result as any).output as string;
                            const lines = output.split('\n').filter(line => line.trim().length > 0);
                            setCliBuildLogs(lines);
                          }
                          if (response.success) {
                            // Get build time from response if available, otherwise fetch it
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                setTimeout(async () => {
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    setCliBuildTime(buildTime);
                                  } catch (error) {
                                    console.error('Failed to get CLI build time:', error);
                                  }
                                }, 1000);
                              }
                            } else {
                              setTimeout(async () => {
                                try {
                                  const buildTime = await invoke<number | null>('get_cli_build_time');
                                  setCliBuildTime(buildTime);
                                } catch (error) {
                                  console.error('Failed to get CLI build time:', error);
                                }
                              }, 1000);
                            }
                            setTimeout(() => {
                              fetchResources(true);
                            }, 1000);
                          }
                        } catch (error) {
                          setCliBuildLogs([`Error: ${error}`]);
                          console.error('Failed to build CLI:', error);
                        } finally {
                          setIsBuildingCli(false);
                        }
                      }}
                      disabled={isBuildingCli}
                      className="px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></span>
                          Building...
                        </>
                      ) : (
                        '🔨 Rebuild CLI'
                      )}
                    </button>
                  )}
                </div>
              </div>
            )}

            {!resourcesLoading && !resourcesError && (
              <ResourceGrid resources={resources} onRefresh={() => fetchResources(true)} />
            )}
          </div>
        )}

        {/* Tab Content: Deployment History */}
        {activeTab === 'history' && (
          <div>
            {deploymentsLoading && (
              <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-8 text-center">
                <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:border-blue-400 mb-3"></div>
                <p className="text-blue-800 dark:text-blue-200">Loading deployment history...</p>
              </div>
            )}

            {deploymentsError && (
              <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-4">
                <h3 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">❌ Failed to Load Deployments</h3>
                <p className="text-red-800 dark:text-red-200 mb-3 whitespace-pre-wrap">{deploymentsError}</p>
                <div className="flex gap-3">
                  <button
                    onClick={() => fetchDeployments(true)}
                    className="px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
                  >
                    Retry
                  </button>
                  {(deploymentsError.includes('Could not find Mystira.DevHub.CLI') ||
                    deploymentsError.includes('Program not found') ||
                    deploymentsError.includes('Failed to spawn process')) && (
                    <button
                      onClick={async () => {
                        setIsBuildingCli(true);
                        setShowCliBuildLogs(true);
                        setCliBuildLogs([]);
                        try {
                          const response = await invoke<CommandResponse>('build_cli');
                          // Parse output from result
                          if (response.result && typeof response.result === 'object' && 'output' in response.result) {
                            const output = (response.result as any).output as string;
                            const lines = output.split('\n').filter(line => line.trim().length > 0);
                            setCliBuildLogs(lines);
                          }
                          if (response.success) {
                            // Get build time from response if available, otherwise fetch it
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                setTimeout(async () => {
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    setCliBuildTime(buildTime);
                                  } catch (error) {
                                    console.error('Failed to get CLI build time:', error);
                                  }
                                }, 1000);
                              }
                            } else {
                              setTimeout(async () => {
                                try {
                                  const buildTime = await invoke<number | null>('get_cli_build_time');
                                  setCliBuildTime(buildTime);
                                } catch (error) {
                                  console.error('Failed to get CLI build time:', error);
                                }
                              }, 1000);
                            }
                            setTimeout(() => {
                              fetchDeployments(true);
                            }, 1000);
                          }
                        } catch (error) {
                          setCliBuildLogs([`Error: ${error}`]);
                          console.error('Failed to build CLI:', error);
                        } finally {
                          setIsBuildingCli(false);
                        }
                      }}
                      disabled={isBuildingCli}
                      className="px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></span>
                          Building...
                        </>
                      ) : (
                        '🔨 Rebuild CLI'
                      )}
                    </button>
                  )}
                </div>
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
}

export default InfrastructurePanel;
