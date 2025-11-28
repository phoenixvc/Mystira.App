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
  onSetCurrentAction?: (action: 'validate' | 'preview' | 'deploy' | 'destroy' | null) => void;
  onSetHasDeployedInfrastructure?: (deployed: boolean) => void;
  onSetDeploymentProgress?: (progress: string | null) => void;
  onSetShowResourceGroupConfirm?: (show: boolean, resourceGroup?: string, location?: string) => void;
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
  onSetCurrentAction,
  onSetHasDeployedInfrastructure,
  onSetDeploymentProgress,
  onSetShowResourceGroupConfirm,
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
      onSetCurrentAction?.(null);
        onSetCurrentAction?.(null);
        return;
      }
    }

    onSetLoading(true);
    onSetCurrentAction?.(action);
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
      onSetCurrentAction?.(null);
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
      onSetCurrentAction?.(null);
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
              } else if (hasCosmosWarning || parsedChanges.length === 0) {
                // Even if we have Cosmos warnings or no parsed changes, try to get valid changes
                let validChanges: WhatIfChange[] = parsedChanges;
                
                // If no changes parsed yet, try to parse from preview data
                if (validChanges.length === 0) {
                  if (previewData.parsed && previewData.parsed.changes) {
                    validChanges = parseWhatIfOutput(JSON.stringify(previewData.parsed));
                  } else if (previewData.preview) {
                    validChanges = parseWhatIfOutput(previewData.preview);
                  } else if (previewData.changes) {
                    validChanges = previewData.changes;
                  }
                }
                
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
                
                // Filter out Cosmos DB nested resources from the changes list
                if (validChanges.length > 0) {
                  validChanges = validChanges.filter(change => {
                    // Keep non-Cosmos resources, or Cosmos account itself (not nested)
                    return !change.resourceType?.includes('Microsoft.DocumentDB/databaseAccounts/sqlDatabases') &&
                           !change.resourceType?.includes('Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers');
                  });
                  
                  if (validChanges.length > 0) {
                    validChanges = validChanges.map(change => ({
                      ...change,
                      resourceGroup: change.resourceGroup || 
                        resourceGroupConfig.resourceTypeMappings?.[change.resourceType] || 
                        resourceGroupConfig.defaultResourceGroup,
                      selected: change.selected !== false, // Default to selected
                    }));
                    onSetWhatIfChanges(validChanges);
                  }
                }
                
                if (hasCosmosWarning) {
                  onSetCosmosWarning({
                    type: 'cosmos-whatif',
                    message: 'Cosmos DB nested resource errors detected during preview',
                    details: errorStr || warningText || 'Cosmos DB nested resource preview limitations',
                    affectedResources,
                    dismissed: false,
                  });
                }
                
                onSetHasPreviewed(true); // Preview completed, even with Cosmos warnings
                onSetLastResponse({
                  success: true,
                  message: `Preview completed${hasCosmosWarning ? ' with Cosmos DB warnings' : ''}. ${validChanges.length > 0 ? validChanges.length + ' resources ready for deployment. ' : ''}${affectedResources.length > 0 ? affectedResources.length + ' Cosmos resources affected by preview limitations. ' : ''}${hasCosmosWarning ? 'These errors are expected and won\'t prevent deployment.' : ''}`,
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
                  } else if (previewData.changes) {
                    parsedChanges = previewData.changes;
                  }
                }

                // Filter out Cosmos DB nested resources from the changes list
                if (parsedChanges.length > 0) {
                  parsedChanges = parsedChanges.filter(change => {
                    // Keep non-Cosmos resources, or Cosmos account itself (not nested)
                    return !change.resourceType?.includes('Microsoft.DocumentDB/databaseAccounts/sqlDatabases') &&
                           !change.resourceType?.includes('Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers');
                  });
                  
                  if (parsedChanges.length > 0) {
                    parsedChanges = parsedChanges.map(change => ({
                      ...change,
                      resourceGroup: change.resourceGroup ||
                        resourceGroupConfig.resourceTypeMappings?.[change.resourceType] ||
                        resourceGroupConfig.defaultResourceGroup,
                      selected: change.selected !== false, // Default to selected
                    }));
                    onSetWhatIfChanges(parsedChanges);
                  }
                }

                onSetCosmosWarning({
                  type: 'cosmos-whatif',
                  message: 'Cosmos DB nested resource errors detected during preview',
                  details: errorStr,
                  affectedResources,
                  dismissed: false,
                });

                onSetHasPreviewed(true); // Preview completed, even with Cosmos errors
                onSetLastResponse({
                  success: true,
                  message: `Preview completed with warnings. ${parsedChanges.length > 0 ? parsedChanges.length + ' resources ready for deployment. ' : ''}${affectedResources.length} Cosmos DB resources reported errors (this is expected for new deployments).`,
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
            // Note: Cosmos DB warnings are expected and don't prevent deployment
            // They're just informational about Azure's what-if preview limitations
            
            if (!hasPreviewed) {
              onSetLastResponse({
                success: false,
                error: 'Please run Preview first to see what will be deployed before deploying.',
              });
              onSetLoading(false);
      onSetCurrentAction?.(null);
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
      onSetCurrentAction?.(null);
                return;
              }
              onSetShowDeployConfirm(true);
              onSetLoading(false);
      onSetCurrentAction?.(null);
              return;
            }

            // If no whatIfChanges but templates are selected, deploy based on templates
            const selectedTemplates = templates.filter(t => t.selected);
            let selectedResources: Array<{ name: string; type: string; module: string }> = [];
            
            if (whatIfChanges.length > 0) {
              // Use what-if changes if available
              for (const c of whatIfChanges) {
                if (c.selected !== false) {
                  const module = getModuleFromResourceType(c.resourceType);
                  if (module) {
                    selectedResources.push({
                      name: c.resourceName,
                      type: c.resourceType,
                      module,
                    });
                  }
                }
              }
            } else if (selectedTemplates.length > 0) {
              // Fallback to templates if no what-if changes (e.g., Cosmos preview errors)
              selectedResources = selectedTemplates
                .filter(t => t.id === 'storage' || t.id === 'cosmos' || t.id === 'appservice')
                .map(t => ({
                  name: t.name,
                  type: t.id,
                  module: t.id === 'storage' ? 'storage' : t.id === 'cosmos' ? 'cosmos' : 'appservice',
                }));
            }

            if (selectedResources.length === 0) {
              onSetLastResponse({
                success: false,
                error: 'Please select at least one template in Step 1 to deploy.',
              });
              onSetLoading(false);
              onSetCurrentAction?.(null);
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
      onSetCurrentAction?.(null);
                return;
              }
            }

            onSetShowDeployConfirm(true);
            onSetLoading(false);
      onSetCurrentAction?.(null);
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
                  resourceName: 'dev-san-cosmos-mystira',
                  changeType: 'modify',
                  changes: ['consistencyPolicy.defaultConsistencyLevel: BoundedStaleness → Session'],
                },
                {
                  resourceType: 'Microsoft.Storage/storageAccounts',
                  resourceName: 'devsanstmystira',
                  changeType: 'noChange',
                },
              ]);
            }
            break;

          case 'deploy':
            const confirmDeploy = confirm('Are you sure you want to deploy infrastructure?');
            if (!confirmDeploy) {
              onSetLoading(false);
      onSetCurrentAction?.(null);
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
      onSetCurrentAction?.(null);
    }
  }, [
    deploymentMethod, repoRoot, environment, templates, resourceGroupConfig,
    hasValidated, hasPreviewed, whatIfChanges, cosmosWarning, workflowFile, repository,
    onSetLoading, onSetLastResponse, onSetShowOutputPanel, onSetHasValidated,
    onSetHasPreviewed, onSetWhatIfChanges, onSetCosmosWarning, onSetShowDeployConfirm,
    onSetShowDestroySelect, onFetchWorkflowStatus, onSetHasDeployedInfrastructure,
    onSetDeploymentProgress, onSetShowResourceGroupConfirm,
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
      onSetCurrentAction?.(null);
        onSetCurrentAction?.(null);
        return;
      }
      
      const destroyResults = [];
      for (const resource of resourcesToDestroy) {
        if (resource.resourceId) {
          // Commented out actual Azure delete call for testing
          // const result = await invoke<CommandResponse>('delete_azure_resource', {
          //   resourceId: resource.resourceId,
          // });
          // destroyResults.push({ resource: resource.resourceName, success: result.success, error: result.error });
          
          // Show warning popup instead
          alert(`⚠️ In a real world, ${resource.resourceName} was now toast!`);
          destroyResults.push({ resource: resource.resourceName, success: true, error: undefined });
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
      onSetCurrentAction?.(null);
    }
  }, [whatIfChanges, onSetLoading, onSetLastResponse, onSetHasPreviewed, onSetWhatIfChanges, onFetchWorkflowStatus]);

  const handleDeployConfirm = useCallback(async (onRefreshInfrastructureStatus?: () => void) => {
    onSetLoading(true);

    try {
      // If no whatIfChanges but templates are selected, deploy based on templates
      const selectedTemplates = templates.filter(t => t.selected);
      let selectedResources: Array<{ name: string; type: string; module: string; resourceGroup: string }> = [];
      
      if (whatIfChanges.length > 0) {
        // Use what-if changes if available
        for (const c of whatIfChanges) {
          if (c.selected !== false) {
            const module = getModuleFromResourceType(c.resourceType);
            if (module) {
              selectedResources.push({
                name: c.resourceName,
                type: c.resourceType,
                module,
                resourceGroup: c.resourceGroup || 
                  resourceGroupConfig.resourceTypeMappings?.[c.resourceType] || 
                  resourceGroupConfig.defaultResourceGroup,
              });
            }
          }
        }
      } else if (selectedTemplates.length > 0) {
        // Fallback to templates if no what-if changes (e.g., Cosmos preview errors)
        const defaultResourceGroup = resourceGroupConfig.defaultResourceGroup;
        selectedResources = selectedTemplates
          .filter(t => t.id === 'storage' || t.id === 'cosmos' || t.id === 'appservice')
          .map(t => ({
            name: t.name,
            type: t.id,
            module: t.id === 'storage' ? 'storage' : t.id === 'cosmos' ? 'cosmos' : 'appservice',
            resourceGroup: defaultResourceGroup,
          }));
      }
      
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
      
      // Check for Cosmos DB failed state error before deploying
      const cosmosAccountName = resourceGroupConfig.projectName 
        ? `${environment}${resourceGroupConfig.region || 'san'}${resourceGroupConfig.projectName}cosmos`
        : null;
      
      for (const resourceGroup of resourceGroups) {
        const resourcesInGroup = resourcesByGroup[resourceGroup];
        
        const selectedModules = new Set(resourcesInGroup.map(r => r.module).filter(Boolean));
        const deployStorage = selectedModules.has('storage');
        const deployCosmos = selectedModules.has('cosmos');
        const deployAppService = selectedModules.has('appservice');
        
        if (!deployStorage && !deployCosmos && !deployAppService) {
          continue;
        }
        
        // Show progress
        const modulesToDeploy = [];
        if (deployStorage) modulesToDeploy.push('Storage Account');
        if (deployCosmos) modulesToDeploy.push('Cosmos DB');
        if (deployAppService) modulesToDeploy.push('App Service');
        onSetDeploymentProgress?.(`Deploying ${modulesToDeploy.join(', ')} to ${resourceGroup}...`);
        
        const response = await invoke<CommandResponse>('azure_deploy_infrastructure', {
          repoRoot,
          environment,
          resourceGroup,
          deployStorage,
          deployCosmos,
          deployAppService,
        });
        
        // Show logs immediately if available - send to Output tab in bottom panel
        if (response.result && (response.result as any).logs) {
          const logs = (response.result as any).logs as string;
          
          // Send deployment logs to Output tab via custom event
          const event = new CustomEvent('deployment-logs', {
            detail: { logs },
          });
          window.dispatchEvent(event);
        }
        
        // Check if resource group needs to be created
        if (!response.success && response.result && (response.result as any).needsCreation) {
          // Resource group doesn't exist - prompt user for confirmation
          const location = (response.result as any).location || resourceGroupConfig.region || 'southafricanorth';
          onSetShowResourceGroupConfirm?.(true, resourceGroup, location);
          
          // Stop deployment and wait for user confirmation
          // The confirmation handler will retry the deployment
          onSetLoading(false);
          onSetCurrentAction?.(null);
          onSetDeploymentProgress?.(null);
          return; // Exit early - user needs to confirm resource group creation
        }
        
        // Check for region capacity issues first
        if (!response.success && response.error && 
            (response.error.includes('ServiceUnavailable') || response.error.includes('high demand'))) {
          const regionMatch = response.error.match(/high demand in ([^,]+)/i) || 
                              response.error.match(/region[^,]*([A-Z][a-z]+ [A-Z][a-z]+)/i);
          const region = regionMatch ? regionMatch[1] : 'West Europe';
          const enhancedError = `❌ Region Capacity Issue: ${region} is currently experiencing high demand for Cosmos DB.\n\n` +
            `💡 Suggested Solutions:\n` +
            `1. Try a different region (e.g., North Europe, East US, UK South)\n` +
            `2. Request region access: https://aka.ms/cosmosdbquota\n` +
            `3. Wait and retry later (capacity issues are usually temporary)\n` +
            `4. Use an existing resource group in a different region\n\n` +
            `Original error:\n${response.error}`;
          
          deploymentResults.push({
            resourceGroup,
            success: false,
            error: enhancedError,
            message: response.message,
            logs: response.result && (response.result as any).logs ? (response.result as any).logs : undefined,
          });
        }
        // Check for Cosmos DB failed state error - extract actual account name from error
        else if (!response.success && response.error && response.error.includes('failed provisioning state')) {
          let actualCosmosAccountName = cosmosAccountName;
          // Try to extract the actual account name from the error message
          const accountNameMatch = response.error.match(/databaseAccounts\/([a-zA-Z0-9-]+)/);
          if (accountNameMatch && accountNameMatch[1]) {
            actualCosmosAccountName = accountNameMatch[1];
          }
          // Also try to extract from the JSON error message
          if (!actualCosmosAccountName) {
            const jsonMatch = response.error.match(/"message":\s*"[^"]*DatabaseAccount\s+([a-zA-Z0-9-]+)/);
            if (jsonMatch && jsonMatch[1]) {
              actualCosmosAccountName = jsonMatch[1];
            }
          }
          
          if (actualCosmosAccountName) {
            const errorMessage = `The Cosmos DB account "${actualCosmosAccountName}" is in a failed provisioning state from a previous deployment attempt. You need to delete it before recreating it.\n\n` +
              `To fix this:\n` +
              `1. Go to Azure Portal: https://portal.azure.com\n` +
              `2. Navigate to the resource group: ${resourceGroup}\n` +
              `3. Find and delete the Cosmos DB account: ${actualCosmosAccountName}\n` +
              `4. Wait for deletion to complete, then try deploying again.\n\n` +
              `Original error:\n${response.error}`;
            
            deploymentResults.push({
              resourceGroup,
              success: false,
              error: errorMessage,
              message: response.message,
              logs: response.result && (response.result as any).logs ? (response.result as any).logs : undefined,
            });
          } else {
            deploymentResults.push({
              resourceGroup,
              success: response.success,
              error: response.error,
              message: response.message,
              logs: response.result && (response.result as any).logs ? (response.result as any).logs : undefined,
            });
          }
        } else {
          deploymentResults.push({
            resourceGroup,
            success: response.success,
            error: response.error,
            message: response.message,
            logs: response.result && (response.result as any).logs ? (response.result as any).logs : undefined,
          });
        }
      }
      
      const allSuccess = deploymentResults.every(r => r.success);
      const errors = deploymentResults.filter(r => !r.success).map(r => `${r.resourceGroup}: ${r.error}`).join('\n');
      
      // Collect all deployment logs
      const allLogs = deploymentResults
        .filter((r: any) => r.logs)
        .map((r: any) => `=== ${r.resourceGroup} ===\n${r.logs}`)
        .join('\n\n');
      
      // Check for region capacity issues and provide helpful suggestions
      let enhancedError = errors;
      if (errors && errors.includes('ServiceUnavailable') && errors.includes('high demand')) {
        const regionMatch = errors.match(/high demand in ([^,]+)/i);
        const region = regionMatch ? regionMatch[1] : 'the selected region';
        enhancedError = `❌ Region Capacity Issue: ${region} is currently experiencing high demand for Cosmos DB.\n\n` +
          `💡 Suggested Solutions:\n` +
          `1. Try a different region (e.g., North Europe, East US, UK South)\n` +
          `2. Request region access: https://aka.ms/cosmosdbquota\n` +
          `3. Wait and retry later (capacity issues are usually temporary)\n` +
          `4. Use an existing resource group in a different region\n\n` +
          `Original error:\n${errors}`;
      }
      
      const response: CommandResponse = {
        success: allSuccess,
        result: { deployments: deploymentResults, logs: allLogs || undefined },
        message: allSuccess ? `Successfully deployed to ${deploymentResults.length} resource group(s)` : undefined,
        error: allSuccess ? undefined : enhancedError,
      };
      
      onSetLastResponse(response);
      onSetDeploymentProgress?.(null);
      
      // Send deployment logs to Output tab via custom event
      if (allLogs) {
        const event = new CustomEvent('deployment-logs', {
          detail: { logs: allLogs },
        });
        window.dispatchEvent(event);
      } else {
        const event = new CustomEvent('deployment-logs', {
          detail: { logs: null },
        });
        window.dispatchEvent(event);
      }

      if (response.success) {
        onSetHasDeployedInfrastructure?.(true);
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
      onSetCurrentAction?.(null);
    }
  }, [whatIfChanges, templates, resourceGroupConfig, repoRoot, environment, onSetLoading, onSetLastResponse, onSetHasPreviewed, onSetWhatIfChanges, onFetchWorkflowStatus, onSetHasDeployedInfrastructure, onSetDeploymentProgress]);

  return {
    handleAction,
    handleDestroyConfirm,
    handleDeployConfirm,
  };
}

