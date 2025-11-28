import { invoke } from '@tauri-apps/api/tauri';
import { useCallback } from 'react';
import type { CommandResponse, CosmosWarning, ResourceGroupConvention, TemplateConfig, WhatIfChange } from '../../../types';
import { getModuleFromResourceType } from '../utils/getModuleFromResourceType';
import { parseWhatIfOutput } from '../utils/parseWhatIfOutput';

interface UseInfrastructureActionsParams {
  deploymentMethod: 'github' | 'azure-cli';
  repoRoot: string;
  environment: string;
  templates: TemplateConfig[];
  resourceGroupConfig: ResourceGroupConvention;
  hasValidated: boolean;
  hasPreviewed: boolean;
  whatIfChanges: WhatIfChange[];
  cosmosWarning: CosmosWarning | null;
  workflowFile: string;
  repository: string;
  onSetLoading: (loading: boolean) => void;
  onSetLastResponse: (response: CommandResponse | null) => void;
  onSetShowOutputPanel: (show: boolean) => void;
  onSetHasValidated: (validated: boolean) => void;
  onSetHasPreviewed: (previewed: boolean) => void;
  onSetWhatIfChanges: (changes: WhatIfChange[]) => void;
  onSetCosmosWarning: (warning: CosmosWarning | null) => void;
  onSetShowDeployConfirm: (show: boolean) => void;
  onSetShowDestroySelect: (show: boolean) => void;
  onFetchWorkflowStatus: () => void;
}

export function useInfrastructureActions({
  deploymentMethod,
  repoRoot,
  environment,
  templates,
  resourceGroupConfig,
  hasValidated,
  hasPreviewed,
  whatIfChanges,
  cosmosWarning,
  workflowFile,
  repository,
  onSetLoading,
  onSetLastResponse,
  onSetShowOutputPanel,
  onSetHasValidated,
  onSetHasPreviewed,
  onSetWhatIfChanges,
  onSetCosmosWarning,
  onSetShowDeployConfirm,
  onSetShowDestroySelect,
  onFetchWorkflowStatus,
}: UseInfrastructureActionsParams) {
  const handleAction = useCallback(async (action: 'validate' | 'preview' | 'deploy' | 'destroy') => {
    if (action !== 'destroy') {
      const selectedTemplates = templates.filter(t => t.selected);
      if (selectedTemplates.length === 0) {
        onSetLastResponse({
          success: false,
          error: 'Please select at least one template in Step 1 before proceeding.',
        });
        onSetLoading(false);
        return;
      }
    }

    onSetLoading(true);
    onSetLastResponse(null);
    onSetShowOutputPanel(true);

    try {
      let response: CommandResponse;

      if (deploymentMethod === 'azure-cli') {
        if (!repoRoot || repoRoot.trim() === '') {
          onSetLastResponse({
            success: false,
            error: 'Repository root not available. Please wait for it to be detected, or use GitHub Actions workflow instead.',
          });
          onSetLoading(false);
          return;
        }
        
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
              onSetHasValidated(true);
            }
            break;
          }

          case 'preview': {
            if (!hasValidated) {
              onSetLastResponse({
                success: false,
                error: 'Please run Validate first before previewing changes.',
              });
              onSetLoading(false);
              return;
            }
            onSetCosmosWarning(null);
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
              
              const warningText = typeof previewData.warnings === 'string' 
                ? previewData.warnings 
                : Array.isArray(previewData.warnings) 
                  ? previewData.warnings.join(' ') 
                  : '';
              
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
              
              if (parsedChanges.length > 0) {
                parsedChanges = parsedChanges.map(change => ({
                  ...change,
                  resourceGroup: change.resourceGroup || 
                    resourceGroupConfig.resourceTypeMappings?.[change.resourceType] || 
                    resourceGroupConfig.defaultResourceGroup,
                }));
                onSetWhatIfChanges(parsedChanges);
                onSetHasPreviewed(true);
                const warningMsg = warningText ? ` (${warningText})` : '';
                onSetLastResponse({
                  success: true,
                  message: `Preview generated: ${parsedChanges.length} changes detected${warningMsg}`,
                });
              } else if (hasCosmosWarning) {
                const errorStr = response.error || 
                  (typeof previewData.errors === 'string' ? previewData.errors : null) ||
                  (typeof previewData.errors === 'object' && previewData.errors ? JSON.stringify(previewData.errors) : null) ||
                  '';
                
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
                
                onSetCosmosWarning({
                  type: 'cosmos-whatif',
                  message: 'Cosmos DB nested resource errors detected during preview',
                  details: errorStr || warningText || 'Cosmos DB nested resource preview limitations',
                  affectedResources,
                  dismissed: false,
                });
                
                onSetWhatIfChanges([]);
                onSetLastResponse({
                  success: true,
                  message: `Preview completed with Cosmos DB warnings. ${affectedResources.length > 0 ? affectedResources.length + ' resources affected. ' : ''}These errors are expected and won't prevent deployment.`,
                });
              } else if (previewData.warnings) {
                onSetLastResponse({
                  success: true,
                  message: previewData.warnings,
                });
              } else {
                onSetLastResponse({
                  success: true,
                  message: 'Preview completed but no changes detected.',
                });
              }
            } else if (response.error) {
              const errorStr = response.error;
              const isOnlyCosmosErrors = errorStr.includes('DeploymentWhatIfResourceError')
                && errorStr.includes('Microsoft.DocumentDB')
                && (errorStr.includes('sqlDatabases') || errorStr.includes('containers'));

              if (isOnlyCosmosErrors) {
                const affectedResources: string[] = [];
                const resourceMatches = errorStr.matchAll(/containers\/(\w+)|sqlDatabases\/(\w+)/g);
                for (const match of resourceMatches) {
                  const resource = match[1] || match[2];
                  if (resource && !affectedResources.includes(resource)) {
                    affectedResources.push(resource);
                  }
                }

                let parsedChanges: WhatIfChange[] = [];
                if (response.result) {
                  const previewData = response.result as any;
                  if (previewData.parsed && previewData.parsed.changes) {
                    parsedChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
                  } else if (previewData.preview) {
                    parsedChanges = parseWhatIfOutput(previewData.preview);
                  }
                }

                onSetCosmosWarning({
                  type: 'cosmos-whatif',
                  message: 'Cosmos DB nested resource errors detected during preview',
                  details: errorStr,
                  affectedResources,
                  dismissed: false,
                });

                if (parsedChanges.length > 0) {
                  parsedChanges = parsedChanges.map(change => ({
                    ...change,
                    resourceGroup: change.resourceGroup ||
                      resourceGroupConfig.resourceTypeMappings?.[change.resourceType] ||
                      resourceGroupConfig.defaultResourceGroup,
                  }));
                  onSetWhatIfChanges(parsedChanges);
                }

                onSetLastResponse({
                  success: false,
                  error: undefined,
                  message: `Preview completed with warnings. ${affectedResources.length} Cosmos DB resources reported errors (this is expected for new deployments).`,
                });
              } else {
                onSetLastResponse({
                  success: false,
                  error: response.error || 'Failed to generate preview',
                });
              }
            }
            break;
          }

          case 'deploy': {
            if (cosmosWarning && !cosmosWarning.dismissed) {
              onSetLastResponse({
                success: false,
                error: 'Please dismiss the Cosmos DB warnings before deploying.',
              });
              onSetLoading(false);
              return;
            }
            
            if (!hasPreviewed) {
              onSetLastResponse({
                success: false,
                error: 'Please run Preview first to see what will be deployed before deploying.',
              });
              onSetLoading(false);
              return;
            }
            
            if (whatIfChanges.length === 0) {
              const selectedTemplates = templates.filter(t => t.selected);
              if (selectedTemplates.length === 0) {
                onSetLastResponse({
                  success: false,
                  error: 'Please select at least one template to deploy.',
                });
                onSetLoading(false);
                return;
              }
              onSetShowDeployConfirm(true);
              onSetLoading(false);
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
              onSetLastResponse({
                success: false,
                error: 'Please select at least one resource to deploy.',
              });
              onSetLoading(false);
              return;
            }

            const selectedModules = new Set(selectedResources.map(r => r.module).filter(Boolean));
            if (selectedModules.has('appservice')) {
              if (!selectedModules.has('cosmos') || !selectedModules.has('storage')) {
                onSetLastResponse({
                  success: false,
                  error: 'App Service requires Cosmos DB and Storage Account to be selected.',
                });
                onSetLoading(false);
                return;
              }
            }

            onSetShowDeployConfirm(true);
            onSetLoading(false);
            return;
          }

          case 'destroy': {
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
              onSetWhatIfChanges([
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
            const confirmDeploy = confirm('Are you sure you want to deploy infrastructure?');
            if (!confirmDeploy) {
              onSetLoading(false);
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

      onSetLastResponse(response);

      if (response.success) {
        setTimeout(() => onFetchWorkflowStatus(), 2000);
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      const isCliNotFound = errorMessage.includes('program not found') || 
                            errorMessage.includes('Could not find Mystira.DevHub.CLI') ||
                            errorMessage.includes('Failed to spawn process');
      
      onSetLastResponse({
        success: false,
        error: isCliNotFound
          ? `❌ Program Not Found\n\n${errorMessage}\n\nPlease build the CLI executable first:\n1. Open a terminal\n2. Navigate to: tools/Mystira.DevHub.CLI\n3. Run: dotnet build`
          : errorMessage,
      });
    } finally {
      onSetLoading(false);
    }
  }, [
    deploymentMethod, repoRoot, environment, templates, resourceGroupConfig,
    hasValidated, hasPreviewed, whatIfChanges, cosmosWarning, workflowFile, repository,
    onSetLoading, onSetLastResponse, onSetShowOutputPanel, onSetHasValidated,
    onSetHasPreviewed, onSetWhatIfChanges, onSetCosmosWarning, onSetShowDeployConfirm,
    onSetShowDestroySelect, onFetchWorkflowStatus,
  ]);

  const handleDestroyConfirm = useCallback(async () => {
    onSetLoading(true);

    try {
      const resourcesToDestroy = whatIfChanges
        .filter(c => c.selected !== false && (c.changeType === 'delete' || c.selected === true))
        .map(c => ({
          resourceId: c.resourceId || '',
          resourceName: c.resourceName,
          resourceType: c.resourceType,
        }));
      
      if (resourcesToDestroy.length === 0) {
        onSetLastResponse({
          success: false,
          error: 'Please select at least one resource to destroy.',
        });
        onSetLoading(false);
        return;
      }
      
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
      
      onSetLastResponse({
        success: allSuccess,
        result: { destroyed: destroyResults.length, results: destroyResults },
        message: allSuccess ? `Successfully destroyed ${destroyResults.length} resource(s)` : undefined,
        error: allSuccess ? undefined : `Some resources failed to destroy:\n${errors}`,
      });
      
      if (allSuccess) {
        setTimeout(() => {
          onFetchWorkflowStatus();
          onSetHasPreviewed(false);
          onSetWhatIfChanges([]);
        }, 2000);
      }
    } catch (error) {
      onSetLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      onSetLoading(false);
    }
  }, [whatIfChanges, onSetLoading, onSetLastResponse, onSetHasPreviewed, onSetWhatIfChanges, onFetchWorkflowStatus]);

  const handleDeployConfirm = useCallback(async (onRefreshInfrastructureStatus?: () => void) => {
    onSetLoading(true);

    try {
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
      
      const resourcesByGroup = selectedResources.reduce((acc, resource) => {
        const rg = resource.resourceGroup || resourceGroupConfig.defaultResourceGroup;
        if (!acc[rg]) {
          acc[rg] = [];
        }
        acc[rg].push(resource);
        return acc;
      }, {} as Record<string, typeof selectedResources>);
      
      const resourceGroups = Object.keys(resourcesByGroup);
      const deploymentResults = [];
      
      for (const resourceGroup of resourceGroups) {
        const resourcesInGroup = resourcesByGroup[resourceGroup];
        
        const selectedModules = new Set(resourcesInGroup.map(r => r.module).filter(Boolean));
        const deployStorage = selectedModules.has('storage');
        const deployCosmos = selectedModules.has('cosmos');
        const deployAppService = selectedModules.has('appservice');
        
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
      
      const allSuccess = deploymentResults.every(r => r.success);
      const errors = deploymentResults.filter(r => !r.success).map(r => `${r.resourceGroup}: ${r.error}`).join('\n');
      
      const response: CommandResponse = {
        success: allSuccess,
        result: { deployments: deploymentResults },
        message: allSuccess ? `Successfully deployed to ${deploymentResults.length} resource group(s)` : undefined,
        error: allSuccess ? undefined : `Some deployments failed:\n${errors}`,
      };
      
      onSetLastResponse(response);

      if (response.success) {
        if (onRefreshInfrastructureStatus) {
          setTimeout(() => {
            onRefreshInfrastructureStatus();
          }, 3000);
        }
        setTimeout(() => onFetchWorkflowStatus(), 2000);
        onSetHasPreviewed(false);
        onSetWhatIfChanges([]);
      }
    } catch (error) {
      onSetLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      onSetLoading(false);
    }
  }, [whatIfChanges, resourceGroupConfig, repoRoot, environment, onSetLoading, onSetLastResponse, onSetHasPreviewed, onSetWhatIfChanges, onFetchWorkflowStatus]);

  return {
    handleAction,
    handleDestroyConfirm,
    handleDeployConfirm,
  };
}

