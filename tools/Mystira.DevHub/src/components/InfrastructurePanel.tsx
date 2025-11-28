import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useRef, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, CosmosWarning, ResourceGroupConvention, StorageAccountConflictWarning, TemplateConfig, WhatIfChange, WorkflowStatus } from '../types';
import { DEFAULT_PROJECTS, type ProjectInfo } from '../types';
import { ConfirmDialog } from './ConfirmDialog';
import DeploymentHistory from './DeploymentHistory';
import InfrastructureStatus, { type InfrastructureStatus as InfrastructureStatusType } from './InfrastructureStatus';
import ProjectDeploymentPlanner from './ProjectDeploymentPlanner';
import ResourceGrid from './ResourceGrid';
import ResourceGroupConfig from './ResourceGroupConfig';
import TemplateEditor from './TemplateEditor';
import TemplateInspector from './TemplateInspector';
import WhatIfViewer from './WhatIfViewer';
import { formatTimeSince } from './services/utils/serviceUtils';
import { extractStorageAccountName, isValidStorageAccountName, parseAzureDeleteError } from './infrastructure/utils/storageAccountUtils';

type Tab = 'actions' | 'templates' | 'resources' | 'history' | 'recommended-fixes';

function InfrastructurePanel() {
  const [activeTab, setActiveTab] = useState<Tab>('actions');
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [workflowStatus, setWorkflowStatus] = useState<WorkflowStatus | null>(null);
  const [whatIfChanges, setWhatIfChanges] = useState<WhatIfChange[]>([]);
  const [showDestroyConfirm, setShowDestroyConfirm] = useState(false);
  const deploymentMethod: 'github' | 'azure-cli' = 'azure-cli';
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [environment, setEnvironment] = useState<string>('dev');
  const [showProdConfirm, setShowProdConfirm] = useState(false);
  const [pendingEnvironment, setPendingEnvironment] = useState<string>('dev');
  const [hasValidated, setHasValidated] = useState(false);
  const [hasPreviewed, setHasPreviewed] = useState(false);
  const [, setHasDeployedInfrastructure] = useState(false);
  const [_hasDeployedInfrastructure, setHasDeployedInfrastructure] = useState(false);
  const [_projects] = useState<ProjectInfo[]>(DEFAULT_PROJECTS);
  const [showDeployConfirm, setShowDeployConfirm] = useState(false);
  const [showOutputPanel, setShowOutputPanel] = useState(false);
  const [showDestroySelect, setShowDestroySelect] = useState(false);
  const [showResourceGroupConfig, setShowResourceGroupConfig] = useState(false);
  const [resourceGroupConfig, setResourceGroupConfig] = useState<ResourceGroupConvention>({
    pattern: '{env}-euw-rg-{resource}',
    defaultResourceGroup: 'dev-euw-rg-mystira-app',
    resourceTypeMappings: {},
  });
  const [templates, setTemplates] = useState<TemplateConfig[]>([
    {
      id: 'storage',
      name: 'Storage Account',
      file: 'storage.bicep',
      description: 'Azure Storage Account with blob services and containers',
      selected: true,
      resourceGroup: '',
      parameters: { sku: 'Standard_LRS' },
    },
    {
      id: 'cosmos',
      name: 'Cosmos DB',
      file: 'cosmos-db.bicep',
      description: 'Azure Cosmos DB account with database and containers',
      selected: true,
      resourceGroup: '',
      parameters: { databaseName: 'MystiraAppDb' },
    },
    {
      id: 'appservice',
      name: 'App Service',
      file: 'app-service.bicep',
      description: 'Azure App Service with Linux runtime',
      selected: true,
      resourceGroup: '',
      parameters: { sku: 'B1' },
    },
    {
      id: 'keyvault',
      name: 'Key Vault',
      file: 'key-vault.bicep',
      description: 'Azure Key Vault for secrets management',
      selected: false,
      resourceGroup: '',
      parameters: {},
    },
  ]);
  const [editingTemplate, setEditingTemplate] = useState<TemplateConfig | null>(null);
  const [step1Collapsed, setStep1Collapsed] = useState(false);
  const [showStep2, setShowStep2] = useState(false);
  const [infrastructureLoading, setInfrastructureLoading] = useState(true);
  const [cosmosWarning, setCosmosWarning] = useState<CosmosWarning | null>(null);
  const [storageAccountConflict, setStorageAccountConflict] = useState<StorageAccountConflictWarning | null>(null);
  const [deletingStorageAccount, setDeletingStorageAccount] = useState(false);
  const [showDeleteStorageConfirm, setShowDeleteStorageConfirm] = useState(false);
  const [autoRetryAfterDelete, setAutoRetryAfterDelete] = useState(false);
  const [fetchingResourceGroup, setFetchingResourceGroup] = useState(false);

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

  // Load resource group config on mount
  useEffect(() => {
    const saved = localStorage.getItem(`resourceGroupConfig_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setResourceGroupConfig(parsed);
      } catch (e) {
        console.error('Failed to parse saved resource group config:', e);
      }
    }
  }, [environment]);

  // Load saved templates on mount
  useEffect(() => {
    const saved = localStorage.getItem(`templates_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setTemplates(parsed);
      } catch (e) {
        console.error('Failed to parse saved templates:', e);
      }
    }
  }, [environment]);

  // Save templates when they change
  useEffect(() => {
    localStorage.setItem(`templates_${environment}`, JSON.stringify(templates));
  }, [templates, environment]);

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

  // Fetch resources when switching to resources tab or environment changes
  useEffect(() => {
    if (activeTab === 'resources') {
      fetchResources(false, environment);
    }
  }, [activeTab, environment, fetchResources]);

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

  // Helper: Query Azure for the storage account's current resource group
  const fetchStorageAccountResourceGroup = async (accountName: string): Promise<string | null> => {
    try {
      const result = await invoke<CommandResponse>('execute_azure_cli', {
        command: `storage account show --name ${accountName} --query resourceGroup --output tsv`,
      });
      if (result.success && result.result) {
        return String(result.result).trim();
      }
    } catch (error) {
      console.error('Failed to fetch storage account resource group:', error);
    }
    return null;
  };

  // Helper: Handle storage account deletion
  const handleDeleteStorageAccount = async () => {
    if (!storageAccountConflict) return;

    const accountName = storageAccountConflict.storageAccountName;

    // Validate account name before using in command
    if (!isValidStorageAccountName(accountName)) {
      setLastResponse({
        success: false,
        error: `Invalid storage account name format: "${accountName}". Expected 3-24 lowercase alphanumeric characters.`,
      });
      setShowDeleteStorageConfirm(false);
      return;
    }

    setDeletingStorageAccount(true);
    setShowDeleteStorageConfirm(false);

    try {
      const result = await invoke<CommandResponse>('execute_azure_cli', {
        command: `storage account delete --name ${accountName} --yes`,
      });

      if (result.success) {
        const deletedAccountName = accountName;
        setStorageAccountConflict(null);
        setLastResponse({
          success: true,
          message: `Storage account "${deletedAccountName}" deleted successfully.${autoRetryAfterDelete ? ' Retrying preview...' : ' You can now retry the preview.'}`,
        });

        // Auto-retry preview if enabled
        if (autoRetryAfterDelete) {
          setAutoRetryAfterDelete(false);
          // Small delay to let user see the success message
          setTimeout(() => {
            handleAction('preview');
          }, 1000);
        }
      } else {
        // Parse error for user-friendly message
        const friendlyError = parseAzureDeleteError(result.error);
        const isAlreadyDeleted = friendlyError.includes('no longer exists');

        setLastResponse({
          success: isAlreadyDeleted, // Treat "already deleted" as success
          message: isAlreadyDeleted ? friendlyError : undefined,
          error: isAlreadyDeleted ? undefined : friendlyError,
        });

        // If already deleted, clear the conflict
        if (isAlreadyDeleted) {
          setStorageAccountConflict(null);
        }
      }
    } catch (error) {
      const errorStr = String(error);
      const friendlyError = parseAzureDeleteError(errorStr);
      setLastResponse({
        success: false,
        error: friendlyError,
      });
    } finally {
      setDeletingStorageAccount(false);
    }
  };

  const handleAction = async (action: 'validate' | 'preview' | 'deploy' | 'destroy') => {
    // Check if templates are selected (except for destroy)
    if (action !== 'destroy') {
      const selectedTemplates = templates.filter(t => t.selected);
      if (selectedTemplates.length === 0) {
        setLastResponse({
          success: false,
          error: 'Please select at least one template in Step 1 before proceeding.',
        });
        setLoading(false);
        return;
      }
    }

    setLoading(true);
    setLastResponse(null);
    setShowOutputPanel(true);

    try {
      let response: CommandResponse;

      if (deploymentMethod === 'azure-cli') {
        // Check if repoRoot is available
        if (!repoRoot || repoRoot.trim() === '') {
          setLastResponse({
            success: false,
            error: 'Repository root not available. Please wait for it to be detected, or use GitHub Actions workflow instead.',
          });
          setLoading(false);
          return;
        }
        
        // Use direct Azure CLI deployment
        switch (action) {
          case 'validate': {
            const selectedTemplates = templates.filter(t => t.selected);
            const deployStorage = selectedTemplates.some(t => t.id === 'storage');
            const deployCosmos = selectedTemplates.some(t => t.id === 'cosmos');
            const deployAppService = selectedTemplates.some(t => t.id === 'appservice');
            
            response = await invoke('azure_validate_infrastructure', {
              repoRoot,
              environment,
              deployStorage,
              deployCosmos,
              deployAppService,
            });
            if (response.success) {
              setHasValidated(true);
            }
            break;
          }

          case 'preview': {
            if (!hasValidated) {
              setLastResponse({
                success: false,
                error: 'Please run Validate first before previewing changes.',
              });
              setLoading(false);
              return;
            }
            // Reset warnings on new preview
            setCosmosWarning(null);
            setStorageAccountConflict(null);
            const selectedTemplates = templates.filter(t => t.selected);
            const deployStorage = selectedTemplates.some(t => t.id === 'storage');
            const deployCosmos = selectedTemplates.some(t => t.id === 'cosmos');
            const deployAppService = selectedTemplates.some(t => t.id === 'appservice');

            response = await invoke('azure_preview_infrastructure', {
              repoRoot,
              environment,
              deployStorage,
              deployCosmos,
              deployAppService,
            });
            if (response.success && response.result) {
              const previewData = response.result as any;
              let parsedChanges: WhatIfChange[] = [];
              
              // Extract warning text (can be string or array)
              const warningText = typeof previewData.warnings === 'string' 
                ? previewData.warnings 
                : Array.isArray(previewData.warnings) 
                  ? previewData.warnings.join(' ') 
                  : '';
              
              // Check for Cosmos DB warnings
              const hasCosmosWarning = previewData.warnings && (
                warningText.includes('Cosmos DB nested resource') ||
                warningText.includes('nested resource errors are expected')
              );
              
              if (previewData.warnings) {
                console.warn('Preview warnings:', previewData.warnings);
              }
              
              if (previewData.parsed && previewData.parsed.changes) {
                parsedChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
              } else if (previewData.preview) {
                parsedChanges = parseWhatIfOutput(previewData.preview);
              } else if (previewData.changes) {
                parsedChanges = previewData.changes;
              }
              
              // Apply resource group mappings to parsed changes
              if (parsedChanges.length > 0) {
                parsedChanges = parsedChanges.map(change => ({
                  ...change,
                  resourceGroup: change.resourceGroup || 
                    resourceGroupConfig.resourceTypeMappings?.[change.resourceType] || 
                    resourceGroupConfig.defaultResourceGroup,
                }));
                setWhatIfChanges(parsedChanges);
                setHasPreviewed(true);
                const warningMsg = warningText ? ` (${warningText})` : '';
                setLastResponse({
                  success: true,
                  message: `Preview generated: ${parsedChanges.length} changes detected${warningMsg}`,
                });
              } else if (hasCosmosWarning) {
                // Cosmos DB nested resource errors are expected - extract affected resources and show warning banner
                // Check all possible places where error details might be stored
                const errorStr = response.error || 
                  (typeof previewData.errors === 'string' ? previewData.errors : null) ||
                  (typeof previewData.errors === 'object' && previewData.errors ? JSON.stringify(previewData.errors) : null) ||
                  '';
                
                // Extract affected resources from error string or warning text
                const affectedResources: string[] = [];
                const searchText = errorStr || warningText || '';
                if (searchText) {
                  const resourceMatches = searchText.matchAll(/containers\/(\w+)|sqlDatabases\/(\w+)/g);
                  for (const match of resourceMatches) {
                    const resource = match[1] || match[2];
                    if (resource && !affectedResources.includes(resource)) {
                      affectedResources.push(resource);
                    }
                  }
                }
                
                setCosmosWarning({
                  type: 'cosmos-whatif',
                  message: 'Cosmos DB nested resource errors detected during preview',
                  details: errorStr || warningText || 'Cosmos DB nested resource preview limitations',
                  affectedResources,
                  dismissed: false,
                });
                
                // Don't mark as previewed yet - wait for user to dismiss warning
                setWhatIfChanges([]);
                setLastResponse({
                  success: true,
                  message: `Preview completed with Cosmos DB warnings. ${affectedResources.length > 0 ? affectedResources.length + ' resources affected. ' : ''}These errors are expected and won't prevent deployment.`,
                });
              } else if (previewData.warnings) {
                // Other warnings - show but don't allow deployment without changes
                setLastResponse({
                  success: true,
                  message: previewData.warnings,
                });
              } else {
                // No changes and no warnings - this shouldn't happen with success=true
                setLastResponse({
                  success: true,
                  message: 'Preview completed but no changes detected.',
                });
              }
            } else if (response.error) {
              // Check if errors are only Cosmos DB nested resource errors (expected)
              const errorStr = response.error;
              const isOnlyCosmosErrors = errorStr.includes('DeploymentWhatIfResourceError')
                && errorStr.includes('Microsoft.DocumentDB')
                && (errorStr.includes('sqlDatabases') || errorStr.includes('containers'));

              if (isOnlyCosmosErrors) {
                // Extract affected resources from error message
                const affectedResources: string[] = [];
                const resourceMatches = errorStr.matchAll(/containers\/(\w+)|sqlDatabases\/(\w+)/g);
                for (const match of resourceMatches) {
                  const resource = match[1] || match[2];
                  if (resource && !affectedResources.includes(resource)) {
                    affectedResources.push(resource);
                  }
                }

                // Try to parse preview data if available
                let parsedChanges: WhatIfChange[] = [];
                if (response.result) {
                  const previewData = response.result as any;
                  if (previewData.parsed && previewData.parsed.changes) {
                    parsedChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
                  } else if (previewData.preview) {
                    parsedChanges = parseWhatIfOutput(previewData.preview);
                  }
                }

                // Set warning state - user can dismiss to continue
                setCosmosWarning({
                  type: 'cosmos-whatif',
                  message: 'Cosmos DB nested resource errors detected during preview',
                  details: errorStr,
                  affectedResources,
                  dismissed: false,
                });

                if (parsedChanges.length > 0) {
                  // Apply resource group mappings
                  parsedChanges = parsedChanges.map(change => ({
                    ...change,
                    resourceGroup: change.resourceGroup ||
                      resourceGroupConfig.resourceTypeMappings?.[change.resourceType] ||
                      resourceGroupConfig.defaultResourceGroup,
                  }));
                  setWhatIfChanges(parsedChanges);
                }

                // Don't mark as previewed yet - wait for user to dismiss warning
                setLastResponse({
                  success: false,
                  error: undefined,
                  message: `Preview completed with warnings. ${affectedResources.length} Cosmos DB resources reported errors (this is expected for new deployments).`,
                });
              } else if (errorStr.includes('StorageAccountInAnotherResourceGroup')) {
                // Extract storage account name using improved helper function
                const storageAccountName = extractStorageAccountName(errorStr);

                if (storageAccountName && isValidStorageAccountName(storageAccountName)) {
                  // Show banner IMMEDIATELY without resource group info
                  setStorageAccountConflict({
                    type: 'storage-account-conflict',
                    message: 'Storage account exists in another resource group',
                    details: errorStr,
                    storageAccountName,
                    currentResourceGroup: undefined, // Will be fetched async
                    dismissed: false,
                  });

                  setLastResponse({
                    success: false,
                    error: undefined,
                    message: `Deployment blocked: Storage account "${storageAccountName}" already exists in a different resource group.`,
                  });

                  // Fetch resource group info asynchronously and update the banner
                  setFetchingResourceGroup(true);
                  fetchStorageAccountResourceGroup(storageAccountName)
                    .then(currentResourceGroup => {
                      // Update existing warning with resource group info
                      setStorageAccountConflict(prev => prev ? {
                        ...prev,
                        currentResourceGroup: currentResourceGroup || undefined,
                      } : null);
                    })
                    .catch(err => {
                      console.error('Failed to fetch resource group:', err);
                      // Banner still shows, just without resource group info
                    })
                    .finally(() => {
                      setFetchingResourceGroup(false);
                    });
                } else {
                  // Could not extract valid storage account name
                  setLastResponse({
                    success: false,
                    error: `Storage account conflict detected but could not extract account name from error. Details: ${errorStr.substring(0, 200)}...`,
                  });
                }
              } else {
                setLastResponse({
                  success: false,
                  error: response.error || 'Failed to generate preview',
                });
              }
            }
            break;
          }

          case 'deploy': {
            // Require preview first - also check if cosmos warning needs to be dismissed
            if (cosmosWarning && !cosmosWarning.dismissed) {
              setLastResponse({
                success: false,
                error: 'Please dismiss the Cosmos DB warnings before deploying.',
              });
              setLoading(false);
              return;
            }
            
            if (!hasPreviewed) {
              setLastResponse({
                success: false,
                error: 'Please run Preview first to see what will be deployed before deploying.',
              });
              setLoading(false);
              return;
            }
            
            // If preview was successful but no changes (e.g., Cosmos DB nested resource scenario),
            // we can still deploy - the preview succeeded, just couldn't show nested resource details
            // In this case, deploy based on selected templates instead of whatIfChanges
            if (whatIfChanges.length === 0) {
              // Preview succeeded but couldn't parse nested resources - deploy based on templates
              const selectedTemplates = templates.filter(t => t.selected);
              if (selectedTemplates.length === 0) {
                setLastResponse({
                  success: false,
                  error: 'Please select at least one template to deploy.',
                });
                setLoading(false);
                return;
              }
              // Proceed with deployment using templates (bypass resource selection)
              setShowDeployConfirm(true);
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
          }

          case 'destroy': {
            // Destroy not implemented for direct Azure CLI yet
            response = {
              success: false,
              error: 'Destroy action not available for direct Azure CLI deployment.',
            };
            break;
          }

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

      // Check if Azure CLI is missing and prompt for installation
      if (!response.success && response.result && typeof response.result === 'object') {
        const result = response.result as any;
        if (result.azureCliMissing && result.wingetAvailable) {
          const shouldInstall = confirm(
            'Azure CLI is not installed. Would you like to install it now using winget?\n\n' +
            'This will open a terminal window to install Azure CLI. After installation, please restart the application.'
          );
          
          if (shouldInstall) {
            try {
              const installResponse = await invoke<CommandResponse>('install_azure_cli');
              if (installResponse.success) {
                const result = installResponse.result as any;
                if (result?.requiresRestart) {
                  alert('A terminal window has opened to install Azure CLI. After installation completes in that window, please RESTART the application for Azure CLI to be detected.\n\nNote: If Azure CLI was already installed, you may need to restart the app for it to be detected in the PATH.');
                } else {
                  alert('A terminal window has opened to install Azure CLI. Please wait for installation to complete in that window, then restart the application.');
                }
              } else {
                alert(`Failed to install Azure CLI: ${installResponse.error || 'Unknown error'}\n\nPlease install manually from https://aka.ms/installazurecliwindows`);
              }
            } catch (error) {
              alert(`Error installing Azure CLI: ${error}\n\nPlease install manually from https://aka.ms/installazurecliwindows`);
            }
          }
        }
      }

      setLastResponse(response);

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
    setShowDestroySelect(false);
    setLoading(true);
    
    try {
      // Get selected resources for destruction (only those with changeType 'delete' or selected)
      const resourcesToDestroy = whatIfChanges
        .filter(c => c.selected !== false && (c.changeType === 'delete' || c.selected === true))
        .map(c => ({
          resourceId: c.resourceId || '',
          resourceName: c.resourceName,
          resourceType: c.resourceType,
        }));
      
      if (resourcesToDestroy.length === 0 && showDestroySelect) {
        setLastResponse({
          success: false,
          error: 'Please select at least one resource to destroy.',
        });
        setLoading(false);
        return;
      }
      
      // If no preview or no selected resources, destroy all (fallback to old behavior)
      if (!showDestroySelect || resourcesToDestroy.length === 0) {
        await handleAction('destroy');
        return;
      }
      
      // Destroy selected resources individually
      const destroyResults = [];
      for (const resource of resourcesToDestroy) {
        if (resource.resourceId) {
          const result = await invoke<CommandResponse>('delete_azure_resource', {
            resourceId: resource.resourceId,
          });
          destroyResults.push({ resource: resource.resourceName, success: result.success, error: result.error });
        }
      }
      
      const allSuccess = destroyResults.every(r => r.success);
      const errors = destroyResults.filter(r => !r.success).map(r => `${r.resource}: ${r.error}`).join('\n');
      
      setLastResponse({
        success: allSuccess,
        result: { destroyed: destroyResults.length, results: destroyResults },
        message: allSuccess ? `Successfully destroyed ${destroyResults.length} resource(s)` : undefined,
        error: allSuccess ? undefined : `Some resources failed to destroy:\n${errors}`,
      });
      
      if (allSuccess) {
        // Refresh resources and reset preview
        setTimeout(() => {
          fetchWorkflowStatus();
          setHasPreviewed(false);
          setWhatIfChanges([]);
        }, 2000);
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

  const handleDeployConfirm = async () => {
    setShowDeployConfirm(false);
    setLoading(true);

    try {
      // Get selected resources with module mapping and resource groups
      const selectedResources = whatIfChanges
        .filter(c => c.selected !== false)
        .map(c => ({
          name: c.resourceName,
          type: c.resourceType,
          module: getModuleFromResourceType(c.resourceType),
          resourceGroup: c.resourceGroup || 
            resourceGroupConfig.resourceTypeMappings?.[c.resourceType] || 
            resourceGroupConfig.defaultResourceGroup,
        }));
      
      // Group resources by resource group
      const resourcesByGroup = selectedResources.reduce((acc, resource) => {
        const rg = resource.resourceGroup || resourceGroupConfig.defaultResourceGroup;
        if (!acc[rg]) {
          acc[rg] = [];
        }
        acc[rg].push(resource);
        return acc;
      }, {} as Record<string, typeof selectedResources>);
      
      // Deploy to each resource group separately
      const resourceGroups = Object.keys(resourcesByGroup);
      const deploymentResults = [];
      
      for (const resourceGroup of resourceGroups) {
        const resourcesInGroup = resourcesByGroup[resourceGroup];
        
        // Extract module flags for resources in this group
        const selectedModules = new Set(resourcesInGroup.map(r => r.module).filter(Boolean));
        const deployStorage = selectedModules.has('storage');
        const deployCosmos = selectedModules.has('cosmos');
        const deployAppService = selectedModules.has('appservice');
        
        // Skip if no modules to deploy
        if (!deployStorage && !deployCosmos && !deployAppService) {
          continue;
        }
        
        const response = await invoke<CommandResponse>('azure_deploy_infrastructure', {
          repoRoot,
          environment,
          resourceGroup,
          deployStorage,
          deployCosmos,
          deployAppService,
        });
        
        deploymentResults.push({
          resourceGroup,
          success: response.success,
          error: response.error,
          message: response.message,
        });
      }
      
      // Combine results
      const allSuccess = deploymentResults.every(r => r.success);
      const errors = deploymentResults.filter(r => !r.success).map(r => `${r.resourceGroup}: ${r.error}`).join('\n');
      
      const response: CommandResponse = {
        success: allSuccess,
        result: { deployments: deploymentResults },
        message: allSuccess ? `Successfully deployed to ${deploymentResults.length} resource group(s)` : undefined,
        error: allSuccess ? undefined : `Some deployments failed:\n${errors}`,
      };
      
      setLastResponse(response);

      if (response.success) {
        // Refresh infrastructure status after deployment
        setTimeout(async () => {
          try {
            const resourceGroup = resourceGroupConfig.defaultResourceGroup || `dev-euw-rg-mystira-app`;
            const statusResponse = await invoke<any>('check_infrastructure_status', {
              environment,
              resourceGroup,
            });
            if (statusResponse.success && statusResponse.result) {
              const status = statusResponse.result as InfrastructureStatusType;
              // Update deployment status based on infrastructure availability
              setHasDeployedInfrastructure(status.available);
              setHasDeployedInfrastructure(status.available);
            }
          } catch (error) {
            console.error('Failed to refresh infrastructure status:', error);
          }
        }, 3000);
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


  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-900 p-0">
      <ConfirmDialog
        isOpen={showDestroyConfirm && !showDestroySelect}
        title="⚠️ Destroy All Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirm}
        onCancel={() => setShowDestroyConfirm(false)}
      />
      <ConfirmDialog
        isOpen={showProdConfirm}
        title="⚠️ Production Environment Warning"
        message="You are about to switch to the PRODUCTION environment. All operations (validate, preview, deploy, destroy) will affect production resources. This is a critical environment with real users and data. Are you absolutely sure you want to proceed?"
        confirmText="Yes, Switch to Production"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="PRODUCTION"
        onConfirm={() => {
          setEnvironment(pendingEnvironment);
          setShowProdConfirm(false);
          setHasValidated(false);
          setHasPreviewed(false);
          setWhatIfChanges([]);
        }}
        onCancel={() => {
          setShowProdConfirm(false);
          setPendingEnvironment(environment);
        }}
      />
      <ConfirmDialog
        isOpen={showDestroySelect}
        title="💥 Destroy Selected Resources"
        message={`You are about to permanently delete ${whatIfChanges.filter(c => c.selected !== false && (c.changeType === 'delete' || c.selected === true)).length} selected resource(s). This action cannot be undone!`}
        confirmText="Yes, Destroy Selected"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirm}
        onCancel={() => setShowDestroySelect(false)}
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
      <div className="p-8 flex-1 flex flex-col min-h-0 w-full">
        <div className="mb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white">
                Infrastructure Control Panel
              </h2>
              {/* Environment and Resource Groups Controls */}
              <div className="flex items-center gap-2">
                <label className="text-sm text-gray-600 dark:text-gray-400">Environment:</label>
                <select
                  value={environment}
                  aria-label="Select environment"
                  onChange={async (e) => {
                    const newEnv = e.target.value;
                    if (newEnv === 'prod') {
                      // Check subscription owner role before allowing prod switch
                      try {
                        const ownerCheck = await invoke<CommandResponse<{ isOwner: boolean; userName: string }>>('check_subscription_owner');
                        if (ownerCheck.success && ownerCheck.result?.isOwner) {
                          setPendingEnvironment(newEnv);
                          setShowProdConfirm(true);
                        } else {
                          alert('Access Denied: You must have Subscription Owner role to switch to production environment.\n\n' +
                                `Current user: ${ownerCheck.result?.userName || 'Unknown'}\n` +
                                'Please contact your subscription administrator.');
                          // Reset dropdown to current environment
                          e.target.value = environment;
                        }
                      } catch (error) {
                        console.error('Failed to check subscription owner:', error);
                        alert('Failed to verify subscription owner role. Cannot switch to production environment.');
                        e.target.value = environment;
                      }
                    } else {
                      setEnvironment(newEnv);
                      setHasValidated(false);
                      setHasPreviewed(false);
                      setWhatIfChanges([]);
                    }
                  }}
                  className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
                >
                  <option value="dev">dev</option>
                  <option value="staging">staging</option>
                  <option value="prod">prod</option>
                </select>
                <button
                  onClick={() => setShowResourceGroupConfig(true)}
                  className="px-4 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-lg text-sm font-medium"
                  title="Configure resource group naming conventions"
                >
                  ⚙️ Resource Groups
                </button>
              </div>
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
                  <div className="flex items-center gap-2">
                    <div className="px-3 py-1.5 rounded-lg bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400 font-mono font-semibold text-sm" 
                         title={`Last CLI build: ${new Date(cliBuildTime).toLocaleString()}`}>
                      {formatTimeSince(cliBuildTime) || 'Unknown'}
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
                            // Get build time from response if available
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                // Build time not in response, fetch it with retries
                                const fetchWithRetry = async (retries = 3) => {
                                  for (let i = 0; i < retries; i++) {
                                    await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                    try {
                                      const buildTime = await invoke<number | null>('get_cli_build_time');
                                      if (buildTime) {
                                        setCliBuildTime(buildTime);
                                        return;
                                      }
                                    } catch (error) {
                                      console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                    }
                                  }
                                };
                                fetchWithRetry();
                              }
                            } else {
                              // No buildTime in response, fetch it with retries
                              const fetchWithRetry = async (retries = 3) => {
                                for (let i = 0; i < retries; i++) {
                                  await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    if (buildTime) {
                                      setCliBuildTime(buildTime);
                                      return;
                                    }
                                  } catch (error) {
                                    console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                  }
                                }
                              };
                              fetchWithRetry();
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
                      title="Rebuild the CLI executable"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                          Building...
                        </>
                      ) : (
                        <>
                          🔨 Rebuild
                        </>
                      )}
                    </button>
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
                            // Get build time from response if available
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                // Build time not in response, fetch it with retries
                                const fetchWithRetry = async (retries = 3) => {
                                  for (let i = 0; i < retries; i++) {
                                    await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                    try {
                                      const buildTime = await invoke<number | null>('get_cli_build_time');
                                      if (buildTime) {
                                        setCliBuildTime(buildTime);
                                        return;
                                      }
                                    } catch (error) {
                                      console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                    }
                                  }
                                };
                                fetchWithRetry();
                              }
                            } else {
                              // No buildTime in response, fetch it with retries
                              const fetchWithRetry = async (retries = 3) => {
                                for (let i = 0; i < retries; i++) {
                                  await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    if (buildTime) {
                                      setCliBuildTime(buildTime);
                                      return;
                                    }
                                  } catch (error) {
                                    console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                  }
                                }
                              };
                              fetchWithRetry();
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
              onClick={() => setActiveTab('templates')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'templates'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              📄 Templates & Resources
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
            <button
              onClick={() => setActiveTab('recommended-fixes')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'recommended-fixes'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              🔧 Recommended Fixes
            </button>
          </nav>
        </div>

        {/* Tab Content: Actions */}
        {activeTab === 'actions' && (
          <div>
            {/* Progress Stepper */}
            <div className="mb-6 px-4 py-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 flex-1">
                  {/* Step 1 */}
                  <div className="flex items-center gap-2">
                    <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
                      templates.some(t => t.selected) 
                        ? 'bg-blue-600 dark:bg-blue-500 text-white' 
                        : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
                    }`}>
                      {templates.some(t => t.selected) ? '✓' : '1'}
                    </div>
                    <span className={`text-sm font-medium ${
                      templates.some(t => t.selected)
                        ? 'text-blue-600 dark:text-blue-400'
                        : 'text-gray-600 dark:text-gray-400'
                    }`}>
                      Plan Deployment
                    </span>
                  </div>
                  
                  {/* Connector - only show when Step 2 is visible */}
                  {showStep2 && (
                    <>
                      <div className={`flex-1 h-0.5 ${
                        templates.some(t => t.selected)
                          ? 'bg-blue-600 dark:bg-blue-500'
                          : 'bg-gray-300 dark:bg-gray-600'
                      }`} />
                      
                      {/* Step 2 */}
                    <div className="flex items-center gap-2">
                      <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
                        hasValidated 
                          ? 'bg-purple-600 dark:bg-purple-500 text-white' 
                          : templates.some(t => t.selected)
                          ? 'bg-blue-600 dark:bg-blue-500 text-white'
                          : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
                      }`}>
                        {hasPreviewed ? '✓' : '2'}
                      </div>
                      <span className={`text-sm font-medium ${
                        hasValidated || templates.some(t => t.selected)
                          ? 'text-blue-600 dark:text-blue-400'
                          : 'text-gray-600 dark:text-gray-400'
                      }`}>
                        Infrastructure Actions
                      </span>
                    </div>
                    </>
                  )}
                </div>
                
                {/* Collapse Toggle */}
                {showStep2 && templates.some(t => t.selected) && (
                  <button
                    onClick={() => setStep1Collapsed(!step1Collapsed)}
                    className="px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 rounded-md border border-gray-300 dark:border-gray-600 transition-colors flex items-center gap-1.5"
                    title={step1Collapsed ? 'Expand Step 1' : 'Collapse Step 1'}
                  >
                    {step1Collapsed ? (
                      <>
                        <span>▼</span>
                        <span>Show Step 1</span>
                      </>
                    ) : (
                      <>
                        <span>▲</span>
                        <span>Hide Step 1</span>
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>

            {/* Infrastructure Status Dashboard */}
            <div className="mb-6">
              <InfrastructureStatus
                environment={environment}
                resourceGroup={resourceGroupConfig.defaultResourceGroup || `dev-euw-rg-mystira-app`}
                onStatusChange={(status) => {
                  // Update deployment status based on infrastructure availability
              setHasDeployedInfrastructure(status.available);
                  setHasDeployedInfrastructure(status.available);
                }}
                onLoadingChange={(loading) => setInfrastructureLoading(loading)}
              />
            </div>

            {/* Project Deployment Planner - Step 1 */}
            {!step1Collapsed && (
              <div className="mb-6">
                <ProjectDeploymentPlanner
                  environment={environment}
                  resourceGroupConfig={resourceGroupConfig}
                  templates={templates}
                  onTemplatesChange={setTemplates}
                  onEditTemplate={setEditingTemplate}
                  region={resourceGroupConfig.region || 'euw'}
                  projectName={resourceGroupConfig.projectName || 'mystira-app'}
                  onProceedToStep2={() => setShowStep2(true)}
                  infrastructureLoading={infrastructureLoading}
                />
              </div>
            )}

            {/* Visual Separator */}
            {showStep2 && templates.some(t => t.selected) && (
              <div className="mb-6 relative">
                <div className="absolute inset-0 flex items-center">
                  <div className="w-full border-t border-gray-300 dark:border-gray-600"></div>
                </div>
                <div className="relative flex justify-center">
                  <span className="px-4 py-1 bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 rounded-full border border-gray-300 dark:border-gray-600">
                    Ready for Step 2
                  </span>
                </div>
              </div>
            )}

            {/* Action Buttons - Step 2 */}
            {showStep2 && (
              <div id="step-2-infrastructure-actions" className="mb-4">
              <div className={`mb-4 ${templates.some(t => t.selected) ? 'sticky top-0 z-10 bg-white dark:bg-gray-900 pb-4 pt-2 -mt-2 border-b border-gray-200 dark:border-gray-700 mb-6' : ''}`}>
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                    Step 2: Infrastructure Actions
                  </h3>
                  {step1Collapsed && (
                    <button
                      onClick={() => setStep1Collapsed(false)}
                      className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium"
                    >
                      ← Back to Step 1
                    </button>
                  )}
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
                  <button
                    onClick={() => handleAction('validate')}
                    disabled={loading}
                    className="px-4 py-3 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                    title="Validate infrastructure templates"
                  >
                    {loading ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    ) : (
                      <>
                        <span>🔍</span>
                        <span>Validate</span>
                      </>
                    )}
                  </button>
                  <button
                    onClick={() => handleAction('preview')}
                    disabled={loading || !hasValidated}
                    className="px-4 py-3 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                    title={!hasValidated ? 'Please validate first' : 'Preview infrastructure changes'}
                  >
                    {loading ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    ) : (
                      <>
                        <span>👁️</span>
                        <span>Preview</span>
                      </>
                    )}
                  </button>
                  <button
                    onClick={() => handleAction('deploy')}
                    disabled={loading || !hasPreviewed}
                    className="px-4 py-3 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                    title={!hasPreviewed ? 'Please preview first' : 'Deploy infrastructure'}
                  >
                    {loading ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    ) : (
                      <>
                        <span>🚀</span>
                        <span>Deploy</span>
                      </>
                    )}
                  </button>
                  <button
                    onClick={() => {
                      if (whatIfChanges.length > 0 && deploymentMethod === 'azure-cli') {
                        setShowDestroySelect(true);
                      } else {
                        handleAction('destroy');
                      }
                    }}
                    disabled={loading}
                    className="px-4 py-3 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                    title="Destroy infrastructure resources"
                  >
                    {loading ? (
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    ) : (
                      <>
                        <span>💥</span>
                        <span>Destroy</span>
                      </>
                    )}
                  </button>
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-400 mt-3">
                  Workflow: Validate → Preview → Deploy (or Destroy)
                </p>
              </div>
            </div>

            {/* Action Buttons Grid */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              {actionButtons.map((button) => (
                <button
                  key={button.id}
                  onClick={button.onClick}
                  disabled={button.disabled}
                  className={`p-4 rounded-lg border-2 transition-all ${
                    button.disabled
                      ? 'opacity-50 cursor-not-allowed bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700'
                      : button.variant === 'primary'
                        ? 'bg-blue-50 dark:bg-blue-900/30 border-blue-200 dark:border-blue-800 hover:bg-blue-100 dark:hover:bg-blue-900/50'
                        : button.variant === 'warning'
                          ? 'bg-yellow-50 dark:bg-yellow-900/30 border-yellow-200 dark:border-yellow-800 hover:bg-yellow-100 dark:hover:bg-yellow-900/50'
                          : button.variant === 'success'
                            ? 'bg-green-50 dark:bg-green-900/30 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/50'
                            : 'bg-red-50 dark:bg-red-900/30 border-red-200 dark:border-red-800 hover:bg-red-100 dark:hover:bg-red-900/50'
                  }`}
                >
                  <div className="text-2xl mb-2">{button.icon}</div>
                  <div className="font-semibold text-gray-900 dark:text-white">{button.label}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-400">{button.description}</div>
                  {button.loading && loading && (
                    <div className="mt-2">
                      <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-current"></span>
                    </div>
                  )}
                </button>
              ))}
            </div>
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
                  <div>
                    <pre className="bg-red-100 dark:bg-red-900/50 p-3 rounded text-sm text-red-900 dark:text-red-200 overflow-auto mb-3">
                      {lastResponse.error}
                    </pre>
                    {(lastResponse.error.includes('Azure CLI is not installed') ||
                      lastResponse.error.includes('Azure CLI not found')) && (
                      <button
                        onClick={async () => {
                          try {
                            const response = await invoke<CommandResponse>('install_azure_cli');
                            if (response.success) {
                              alert('Azure CLI installation started. Please restart the application after installation completes.');
                            } else {
                              alert(`Failed to install Azure CLI: ${response.error || 'Unknown error'}`);
                            }
                          } catch (error) {
                            alert(`Error installing Azure CLI: ${error}`);
                          }
                        }}
                        className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors"
                      >
                        📦 Install Azure CLI
                      </button>
                    )}
                  </div>
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

            {/* Cosmos DB Warning Banner - Dismissible */}
            {cosmosWarning && !cosmosWarning.dismissed && (
              <div className="rounded-lg p-4 mb-6 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700">
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3">
                    <span className="text-amber-500 text-xl">⚠️</span>
                    <div className="flex-1">
                      <h4 className="text-sm font-semibold text-amber-800 dark:text-amber-200 mb-1">
                        Expected Cosmos DB Preview Warnings
                      </h4>
                      <p className="text-xs text-amber-700 dark:text-amber-300 mb-2">
                        Azure's what-if preview cannot predict changes for nested Cosmos DB resources
                        (databases and containers) that don't exist yet. This is a known Azure limitation
                        and does not affect actual deployments.
                      </p>
                      {cosmosWarning.affectedResources.length > 0 && (
                        <div className="text-xs text-amber-600 dark:text-amber-400 mb-2">
                          <span className="font-medium">Affected resources:</span>{' '}
                          {cosmosWarning.affectedResources.join(', ')}
                        </div>
                      )}
                      <details className="text-xs">
                        <summary className="cursor-pointer text-amber-600 dark:text-amber-400 hover:text-amber-800 dark:hover:text-amber-200">
                          View full error details
                        </summary>
                        <pre className="mt-2 p-2 bg-amber-100 dark:bg-amber-900/50 rounded text-[10px] overflow-auto max-h-32 text-amber-800 dark:text-amber-200">
                          {cosmosWarning.details}
                        </pre>
                      </details>
                    </div>
                  </div>
                  <button
                    onClick={() => {
                      setCosmosWarning({ ...cosmosWarning, dismissed: true });
                      setHasPreviewed(true);
                      setLastResponse({
                        success: true,
                        message: `Preview completed. ${whatIfChanges.length} resource changes ready for deployment.`,
                      });
                    }}
                    className="ml-4 px-3 py-1.5 text-xs font-medium bg-amber-100 dark:bg-amber-800 hover:bg-amber-200 dark:hover:bg-amber-700 text-amber-700 dark:text-amber-200 rounded-md transition-colors whitespace-nowrap"
                  >
                    Dismiss & Continue
                  </button>
                </div>
              </div>
            )}

            {/* Storage Account Conflict Warning Banner */}
            {storageAccountConflict && !storageAccountConflict.dismissed && (
              <div className="rounded-lg p-4 mb-6 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700">
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3">
                    <span className="text-red-500 text-xl">🗄️</span>
                    <div className="flex-1">
                      <h4 className="text-sm font-semibold text-red-800 dark:text-red-200 mb-1">
                        Storage Account Conflict
                      </h4>
                      <p className="text-xs text-red-700 dark:text-red-300 mb-2">
                        The storage account <strong>{storageAccountConflict.storageAccountName}</strong> already
                        exists{storageAccountConflict.currentResourceGroup && (
                          <> in resource group <strong>{storageAccountConflict.currentResourceGroup}</strong></>
                        )}. You can delete the existing storage account to proceed with deployment, or dismiss
                        this warning if you want to use a different name.
                      </p>
                      {storageAccountConflict.currentResourceGroup ? (
                        <div className="text-xs text-red-600 dark:text-red-400 mb-2 flex items-center gap-1">
                          <span>📍</span>
                          <span>Current location: <code className="bg-red-100 dark:bg-red-900/50 px-1 rounded">{storageAccountConflict.currentResourceGroup}</code></span>
                        </div>
                      ) : fetchingResourceGroup ? (
                        <div className="text-xs text-red-600 dark:text-red-400 mb-2 flex items-center gap-1">
                          <span className="animate-spin">⏳</span>
                          <span>Looking up resource group...</span>
                        </div>
                      ) : null}
                      <details className="text-xs">
                        <summary className="cursor-pointer text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-200">
                          View full error details
                        </summary>
                        <pre className="mt-2 p-2 bg-red-100 dark:bg-red-900/50 rounded text-[10px] overflow-auto max-h-32 text-red-800 dark:text-red-200">
                          {storageAccountConflict.details}
                        </pre>
                      </details>
                    </div>
                  </div>
                  <div className="flex flex-col gap-2 ml-4">
                    <div className="flex gap-2">
                      <button
                        onClick={() => setShowDeleteStorageConfirm(true)}
                        disabled={deletingStorageAccount}
                        className="px-3 py-1.5 text-xs font-medium bg-red-100 dark:bg-red-800 hover:bg-red-200 dark:hover:bg-red-700 text-red-700 dark:text-red-200 rounded-md transition-colors whitespace-nowrap disabled:opacity-50"
                      >
                        {deletingStorageAccount ? 'Deleting...' : '🗑️ Delete'}
                      </button>
                      <button
                        onClick={() => {
                          setStorageAccountConflict({ ...storageAccountConflict, dismissed: true });
                        }}
                        className="px-3 py-1.5 text-xs font-medium bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-md transition-colors whitespace-nowrap"
                      >
                        Dismiss
                      </button>
                    </div>
                    <label className="flex items-center gap-1.5 text-xs text-red-600 dark:text-red-400 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={autoRetryAfterDelete}
                        onChange={(e) => setAutoRetryAfterDelete(e.target.checked)}
                        className="w-3 h-3 rounded border-red-300 dark:border-red-600 text-red-600 focus:ring-red-500"
                      />
                      <span>Auto-retry preview after delete</span>
                    </label>
                  </div>
                </div>
              </div>
            )}

            {/* Storage Account Delete Confirmation Dialog */}
            <ConfirmDialog
              isOpen={showDeleteStorageConfirm}
              title="Delete Storage Account"
              message={`This will permanently delete the storage account "${storageAccountConflict?.storageAccountName || ''}"${storageAccountConflict?.currentResourceGroup ? ` from resource group "${storageAccountConflict.currentResourceGroup}"` : ''}. All data in the account will be lost. This action cannot be undone.`}
              confirmText="Delete Storage Account"
              cancelText="Cancel"
              confirmButtonClass="bg-red-600 hover:bg-red-700"
              requireTextMatch={storageAccountConflict?.storageAccountName}
              onConfirm={handleDeleteStorageAccount}
              onCancel={() => setShowDeleteStorageConfirm(false)}
            />

            {/* What-If Viewer */}
            {whatIfChanges.length > 0 && (
              <div className="mb-8">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                    Resource Changes Preview
                  </h3>
                  <div className="flex gap-2">
                    <button
                      onClick={() => {
                        const updated = whatIfChanges.map(c => ({ ...c, selected: true }));
                        setWhatIfChanges(updated);
                      }}
                      className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
                    >
                      Select All
                    </button>
                    <button
                      onClick={() => {
                        const updated = whatIfChanges.map(c => ({ ...c, selected: false }));
                        setWhatIfChanges(updated);
                      }}
                      className="px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded"
                    >
                      Deselect All
                    </button>
                  </div>
                </div>
                <WhatIfViewer 
                  changes={whatIfChanges} 
                  loading={loading && activeTab === 'actions'}
                  showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
                  onSelectionChange={(updated) => setWhatIfChanges(updated)}
                  defaultResourceGroup={resourceGroupConfig.defaultResourceGroup}
                  resourceGroupMappings={resourceGroupConfig.resourceTypeMappings || {}}
                />
              </div>
            )}

            {/* Resource Group Config Modal */}
            {showResourceGroupConfig && (
              <ResourceGroupConfig
                environment={environment}
                onSave={(config) => {
                  setResourceGroupConfig(config);
                  // Update existing whatIfChanges with new resource groups
                  const updated = whatIfChanges.map(change => ({
                    ...change,
                    resourceGroup: change.resourceGroup || 
                      config.resourceTypeMappings?.[change.resourceType] || 
                      config.defaultResourceGroup,
                  }));
                  setWhatIfChanges(updated);
                }}
                onClose={() => setShowResourceGroupConfig(false)}
              />
            )}

            {/* Template Editor Modal */}
            {editingTemplate && (
              <TemplateEditor
                template={editingTemplate}
                onSave={(template, saveAsNew) => {
                  if (saveAsNew) {
                    // Add as new template
                    const newTemplate = { ...template, id: `${template.id}-${Date.now()}` };
                    setTemplates([...templates, newTemplate]);
                  } else {
                    // Update existing template
                    const updated = templates.map(t => t.id === template.id ? template : t);
                    setTemplates(updated);
                  }
                  setEditingTemplate(null);
                }}
                onClose={() => setEditingTemplate(null)}
              />
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
              </div>
            )}
          </div>
        )}

        {/* Tab Content: Bicep Viewer */}
        {activeTab === 'templates' && (
          <div className="h-full">
            <TemplateInspector environment={environment} />
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
                <div className="flex gap-3 flex-wrap">
                  <button
                    onClick={() => fetchResources(true, environment)}
                    className="px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
                  >
                    Retry
                  </button>
                  {(resourcesError.includes('Azure CLI is not installed') ||
                    resourcesError.includes('Azure CLI not found')) && (
                    <button
                      onClick={async () => {
                        try {
                          const response = await invoke<CommandResponse>('install_azure_cli');
                          if (response.success) {
                            alert('Azure CLI installation started. Please restart the application after installation completes.');
                          } else {
                            alert(`Failed to install Azure CLI: ${response.error || 'Unknown error'}`);
                          }
                        } catch (error) {
                          alert(`Error installing Azure CLI: ${error}`);
                        }
                      }}
                      className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors"
                    >
                      📦 Install Azure CLI
                    </button>
                  )}
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
                            // Get build time from response if available
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                // Build time not in response, fetch it with retries
                                const fetchWithRetry = async (retries = 3) => {
                                  for (let i = 0; i < retries; i++) {
                                    await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                    try {
                                      const buildTime = await invoke<number | null>('get_cli_build_time');
                                      if (buildTime) {
                                        setCliBuildTime(buildTime);
                                        return;
                                      }
                                    } catch (error) {
                                      console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                    }
                                  }
                                };
                                fetchWithRetry();
                              }
                            } else {
                              // No buildTime in response, fetch it with retries
                              const fetchWithRetry = async (retries = 3) => {
                                for (let i = 0; i < retries; i++) {
                                  await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    if (buildTime) {
                                      setCliBuildTime(buildTime);
                                      return;
                                    }
                                  } catch (error) {
                                    console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                  }
                                }
                              };
                              fetchWithRetry();
                            }
                            setTimeout(() => {
                              fetchResources(true, environment);
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
              <ResourceGrid
                resources={resources}
                onRefresh={() => fetchResources(true, environment)}
                onDelete={async (resourceId: string) => {
                  try {
                    const response = await invoke<CommandResponse>('delete_azure_resource', {
                      resourceId,
                    });
                    if (response.success) {
                      // Refresh resources after successful deletion
                      fetchResources(true, environment);
                    } else {
                      throw new Error(response.error || 'Failed to delete resource');
                    }
                  } catch (error) {
                    throw error;
                  }
                }}
              />
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
                            // Get build time from response if available
                            if (response.result && typeof response.result === 'object' && 'buildTime' in response.result) {
                              const buildTime = (response.result as any).buildTime as number | null;
                              if (buildTime) {
                                setCliBuildTime(buildTime);
                              } else {
                                // Build time not in response, fetch it with retries
                                const fetchWithRetry = async (retries = 3) => {
                                  for (let i = 0; i < retries; i++) {
                                    await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                    try {
                                      const buildTime = await invoke<number | null>('get_cli_build_time');
                                      if (buildTime) {
                                        setCliBuildTime(buildTime);
                                        return;
                                      }
                                    } catch (error) {
                                      console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                    }
                                  }
                                };
                                fetchWithRetry();
                              }
                            } else {
                              // No buildTime in response, fetch it with retries
                              const fetchWithRetry = async (retries = 3) => {
                                for (let i = 0; i < retries; i++) {
                                  await new Promise(resolve => setTimeout(resolve, 1000 + i * 500));
                                  try {
                                    const buildTime = await invoke<number | null>('get_cli_build_time');
                                    if (buildTime) {
                                      setCliBuildTime(buildTime);
                                      return;
                                    }
                                  } catch (error) {
                                    console.error(`Failed to get CLI build time (attempt ${i + 1}):`, error);
                                  }
                                }
                              };
                              fetchWithRetry();
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

        {/* Tab Content: Recommended Fixes */}
        {activeTab === 'recommended-fixes' && (
          <div>
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                🔧 Recommended Fixes & Improvements
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                Security and safety improvements to prevent accidental actions and improve resource management
              </p>
            </div>

            <div className="space-y-4">
              {/* Delete Button Protection */}
              <div className="p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg border border-yellow-200 dark:border-yellow-800">
                <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
                  <span>🛡️</span>
                  Delete Button Protection
                </h4>
                <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
                  Add filters and confirmation requirements to prevent accidental deletion of resources
                </p>
                <div className="flex flex-wrap gap-2">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">Require resource name confirmation before delete</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">Hide delete buttons by default (toggle to show)</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">Filter by environment prefix before allowing delete</span>
                  </label>
                </div>
              </div>

              {/* Environment Switch Security */}
              <div className="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
                <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
                  <span>🔒</span>
                  Environment Switch Security
                </h4>
                <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
                  Require subscription owner permissions for production environment operations
                </p>
                <div className="space-y-2">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={true}
                      readOnly
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">Require subscription owner role for prod-* resources</span>
                  </label>
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">Auto-detect subscription owner and validate before operations</span>
                  </label>
                </div>
              </div>

              {/* Resource Tagging */}
              <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-800">
                <h4 className="font-semibold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
                  <span>🏷️</span>
                  Resource Tagging Script
                </h4>
                <p className="text-sm text-gray-700 dark:text-gray-300 mb-3">
                  Automatically add required tags to Azure resources for better organization and compliance
                </p>
                <div className="space-y-3">
                  <button
                    onClick={async () => {
                      try {
                        const response = await invoke<CommandResponse>('run_resource_tagging_script', {
                          environment: environment === 'prod' ? 'prod' : 'dev',
                          dryRun: true,
                        });
                        if (response.success) {
                          alert('Tagging script ready. Preview mode will show what tags would be added.');
                        } else {
                          alert(`Error: ${response.error}`);
                        }
                      } catch (error) {
                        console.error('Failed to run tagging script:', error);
                        alert('Tagging script feature is not yet implemented in the backend.');
                      }
                    }}
                    className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors text-sm font-medium"
                  >
                    🔍 Preview Tags (Dry Run)
                  </button>
                  <button
                    onClick={async () => {
                      if (!confirm(`Are you sure you want to add tags to all ${environment} resources?`)) {
                        return;
                      }
                      try {
                        const response = await invoke<CommandResponse>('run_resource_tagging_script', {
                          environment: environment === 'prod' ? 'prod' : 'dev',
                          dryRun: false,
                        });
                        if (response.success) {
                          alert('Tags have been successfully added to resources.');
                        } else {
                          alert(`Error: ${response.error}`);
                        }
                      } catch (error) {
                        console.error('Failed to run tagging script:', error);
                        alert('Tagging script feature is not yet implemented in the backend.');
                      }
                    }}
                    className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors text-sm font-medium ml-2"
                  >
                    ✏️ Apply Tags to Resources
                  </button>
                </div>
              </div>
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
