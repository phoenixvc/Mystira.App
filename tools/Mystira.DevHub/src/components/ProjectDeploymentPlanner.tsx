import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import type { CommandResponse, ProjectInfo, ResourceGroupConvention, TemplateConfig } from '../types';
import { DEFAULT_PROJECTS } from '../types';
import type { InfrastructureStatus } from './InfrastructureStatus';
import { formatTimeSince } from './services/utils/serviceUtils';

// Re-export for convenience
export type { ProjectInfo };

export interface DeploymentStatus {
  projectId: string;
  resources: {
    storage: { deployed: boolean; name?: string };
    cosmos: { deployed: boolean; name?: string };
    appService: { deployed: boolean; name?: string };
    keyVault: { deployed: boolean; name?: string };
  };
  allRequiredDeployed: boolean; // True if ALL required resources are deployed
  lastChecked: number | null;
}

interface ProjectDeploymentPlannerProps {
  environment: string;
  resourceGroupConfig: ResourceGroupConvention;
  templates: TemplateConfig[];
  onTemplatesChange: (templates: TemplateConfig[]) => void;
  onEditTemplate: (template: TemplateConfig) => void;
  region?: string;
  projectName?: string;
  onReadyToProceed?: (ready: boolean, reason?: string) => void;
  onProceedToStep2?: () => void;
  infrastructureLoading?: boolean;
}

function ProjectDeploymentPlanner({
  environment,
  resourceGroupConfig,
  templates,
  onTemplatesChange,
  onReadyToProceed,
  onProceedToStep2,
  infrastructureLoading = false,
}: ProjectDeploymentPlannerProps) {
  const [projects] = useState<ProjectInfo[]>(DEFAULT_PROJECTS);

  const [deploymentStatus, setDeploymentStatus] = useState<Record<string, DeploymentStatus>>({});
  const [loadingStatus, setLoadingStatus] = useState(false);
  const [lastRefreshTime, setLastRefreshTime] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDeploymentStatus();
  }, [environment, resourceGroupConfig.defaultResourceGroup]);

  useEffect(() => {
    // Check if ready to proceed to Step 2
    const selectedTemplates = templates.filter(t => t.selected);
    const hasSelectedTemplates = selectedTemplates.length > 0;
    
    if (!hasSelectedTemplates) {
      onReadyToProceed?.(false, 'Please select at least one infrastructure template to deploy.');
      return;
    }
    
    if (infrastructureLoading || loadingStatus) {
      onReadyToProceed?.(false, 'Please wait for infrastructure status to finish loading...');
      return;
    }

    // All validations passed
    onReadyToProceed?.(true);
  }, [templates, onReadyToProceed, infrastructureLoading, loadingStatus]);

  const loadDeploymentStatus = async () => {
    setLoadingStatus(true);
    setError(null);
    try {
      const resourceGroup = resourceGroupConfig.defaultResourceGroup || `dev-euw-rg-mystira-app`;
      
      // Use the proper infrastructure status check
      const response: CommandResponse<InfrastructureStatus> = await invoke('check_infrastructure_status', {
        environment,
        resourceGroup,
      });

      if (response.success && response.result) {
        const infrastructureStatus = response.result;
        const status: Record<string, DeploymentStatus> = {};
        const refreshTime = Date.now();

        projects.forEach(project => {
          // Check each required resource type
          const storageDeployed = project.infrastructure.storage 
            ? (infrastructureStatus.resources.storage?.exists || false)
            : true; // Not required, consider "deployed"
          
          const cosmosDeployed = project.infrastructure.cosmos 
            ? (infrastructureStatus.resources.cosmos?.exists || false)
            : true;
          
          const appServiceDeployed = project.infrastructure.appService 
            ? (infrastructureStatus.resources.appService?.exists || false)
            : true;
          
          const keyVaultDeployed = project.infrastructure.keyVault 
            ? (infrastructureStatus.resources.keyVault?.exists || false)
            : true;

          // Check if ALL required resources are deployed
          const allRequiredDeployed = 
            (!project.infrastructure.storage || storageDeployed) &&
            (!project.infrastructure.cosmos || cosmosDeployed) &&
            (!project.infrastructure.appService || appServiceDeployed) &&
            (!project.infrastructure.keyVault || keyVaultDeployed);

          const projectStatus: DeploymentStatus = {
            projectId: project.id,
            resources: {
              storage: { 
                deployed: storageDeployed, 
                name: infrastructureStatus.resources.storage?.name 
              },
              cosmos: { 
                deployed: cosmosDeployed, 
                name: infrastructureStatus.resources.cosmos?.name 
              },
              appService: { 
                deployed: appServiceDeployed, 
                name: infrastructureStatus.resources.appService?.name 
              },
              keyVault: { 
                deployed: keyVaultDeployed, 
                name: infrastructureStatus.resources.keyVault?.name 
              },
            },
            allRequiredDeployed,
            lastChecked: refreshTime,
          };

          status[project.id] = projectStatus;
        });

        setDeploymentStatus(status);
        setLastRefreshTime(refreshTime);
      } else {
        const errorMsg = response.error || 'Failed to check infrastructure status';
        setError(errorMsg);
        console.error('Failed to load deployment status:', errorMsg);
      }
    } catch (error) {
      const errorMsg = `Error checking infrastructure status: ${error}`;
      setError(errorMsg);
      console.error('Failed to load deployment status:', error);
    } finally {
      setLoadingStatus(false);
    }
  };

  const toggleTemplateForProject = (projectId: string, templateId: string) => {
    const project = projects.find(p => p.id === projectId);
    if (!project) return;

    const template = templates.find(t => t.id === templateId);
    if (!template) return;

    // Toggle template selection
    const updatedTemplates = templates.map(t => {
      if (t.id === templateId) {
        return { ...t, selected: !t.selected };
      }
      return t;
    });

    onTemplatesChange(updatedTemplates);
  };

  const getProjectTypeIcon = (type: string) => {
    switch (type) {
      case 'api': return '🌐';
      case 'admin-api': return '🔧';
      case 'pwa': return '📱';
      case 'service': return '⚙️';
      default: return '📦';
    }
  };

  const getProjectTypeColor = (type: string) => {
    switch (type) {
      case 'api': return 'bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200';
      case 'admin-api': return 'bg-purple-100 dark:bg-purple-900 text-purple-800 dark:text-purple-200';
      case 'pwa': return 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200';
      case 'service': return 'bg-orange-100 dark:bg-orange-900 text-orange-800 dark:text-orange-200';
      default: return 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200';
    }
  };


  const isTemplateSelected = (templateId: string) => {
    const template = templates.find(t => t.id === templateId);
    return template?.selected || false;
  };

  const selectedTemplates = templates.filter(t => t.selected);
  const hasSelectedTemplates = selectedTemplates.length > 0;
  const readyToProceed = hasSelectedTemplates && !infrastructureLoading && !loadingStatus;

  return (
    <div className="mb-8">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-1">
            Step 1: Plan Infrastructure Deployment
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Select infrastructure templates for each project that needs cloud deployment
          </p>
        </div>
        <div className="flex items-center gap-3">
          {lastRefreshTime && (
            <div className="text-xs text-gray-500 dark:text-gray-400">
              Last refreshed: {formatTimeSince(lastRefreshTime)}
            </div>
          )}
          <button
            onClick={loadDeploymentStatus}
            disabled={loadingStatus}
            className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded disabled:opacity-50"
          >
            {loadingStatus ? '🔄 Loading...' : '🔄 Refresh Status'}
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
          <p className="text-sm text-red-700 dark:text-red-300">{error}</p>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {projects.map((project) => {
          const status = deploymentStatus[project.id];
          // Check if ALL required resources for this project are deployed
          const isFullyDeployed = status?.allRequiredDeployed || false;
          const hasAnyDeployed = status && Object.values(status.resources).some(r => r.deployed);

          // Determine required resources for this project
          const requiredResources = [];
          if (project.infrastructure.storage) requiredResources.push('storage');
          if (project.infrastructure.cosmos) requiredResources.push('cosmos');
          if (project.infrastructure.appService) requiredResources.push('appService');
          if (project.infrastructure.keyVault) requiredResources.push('keyVault');

          return (
            <div
              key={project.id}
              className={`border-2 rounded-lg p-4 transition-all flex flex-col h-full ${
                isFullyDeployed
                  ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/20'
                  : hasAnyDeployed
                  ? 'border-yellow-200 dark:border-yellow-800 bg-yellow-50 dark:bg-yellow-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
              }`}
            >
              <div className="flex items-start gap-3 mb-3">
                <div className="flex-shrink-0">
                  <div className={`w-12 h-12 rounded-lg flex items-center justify-center text-2xl ${getProjectTypeColor(project.type)}`}>
                    {getProjectTypeIcon(project.type)}
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2 mb-1">
                    <h4 className="font-semibold text-gray-900 dark:text-white truncate">{project.name}</h4>
                    <span className={`px-2 py-0.5 text-xs rounded flex-shrink-0 ${getProjectTypeColor(project.type)}`}>
                      {project.type}
                    </span>
                    {isFullyDeployed && (
                      <span className="px-2 py-0.5 text-xs rounded bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300 flex-shrink-0">
                        ✓ Fully Deployed
                      </span>
                    )}
                    {status && (() => {
                      const deployedResources: string[] = [];
                      const missingResources: string[] = [];
                      
                      if (project.infrastructure.storage) {
                        if (status.resources.storage.deployed) {
                          deployedResources.push('Storage');
                        } else {
                          missingResources.push('Storage');
                        }
                      }
                      if (project.infrastructure.cosmos) {
                        if (status.resources.cosmos.deployed) {
                          deployedResources.push('Cosmos DB');
                        } else {
                          missingResources.push('Cosmos DB');
                        }
                      }
                      if (project.infrastructure.appService) {
                        if (status.resources.appService.deployed) {
                          deployedResources.push('App Service');
                        } else {
                          missingResources.push('App Service');
                        }
                      }
                      if (project.infrastructure.keyVault) {
                        if (status.resources.keyVault.deployed) {
                          deployedResources.push('Key Vault');
                        } else {
                          missingResources.push('Key Vault');
                        }
                      }
                      
                      // Show badge based on deployment status
                      if (deployedResources.length === 0 && missingResources.length > 0) {
                        // Nothing deployed - show "Not Deployed" badge
                        const tooltipText = `Not Deployed\nMissing: ${missingResources.join(', ')}`;
                        
                        return (
                          <span 
                            className="px-2 py-0.5 text-xs rounded bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 flex-shrink-0"
                            title={tooltipText}
                          >
                            ❌ Not Deployed
                          </span>
                        );
                      }
                      
                      if (deployedResources.length > 0 && missingResources.length > 0) {
                        // Some deployed, some missing - partially deployed
                        const tooltipText = `Partially Deployed\nDeployed: ${deployedResources.join(', ')}\nMissing: ${missingResources.join(', ')}`;
                        
                        return (
                          <span 
                            className="px-2 py-0.5 text-xs rounded bg-yellow-100 dark:bg-yellow-900 text-yellow-700 dark:text-yellow-300 flex-shrink-0"
                            title={tooltipText}
                          >
                            ⚠ Partially Deployed
                          </span>
                        );
                      }
                      
                      return null;
                    })()}
                  </div>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{project.description}</p>
                </div>
              </div>
              
              {/* Infrastructure Requirements - stacked vertically */}
              <div className="flex flex-col gap-2 mt-auto">
                {project.infrastructure.storage && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={isTemplateSelected('storage')}
                      onChange={() => toggleTemplateForProject(project.id, 'storage')}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 flex-shrink-0"
                      aria-label={`Select storage for ${project.name}`}
                    />
                    <span className="text-xs text-gray-700 dark:text-gray-300 flex-1">Storage</span>
                    {status?.resources.storage.deployed ? (
                      <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.storage.name}>
                        ✓
                      </span>
                    ) : (
                      <span className="text-xs text-red-600 dark:text-red-400">
                        ✗ Not Deployed
                      </span>
                    )}
                  </div>
                )}
                {project.infrastructure.cosmos && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={isTemplateSelected('cosmos')}
                      onChange={() => toggleTemplateForProject(project.id, 'cosmos')}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 flex-shrink-0"
                      aria-label={`Select Cosmos DB for ${project.name}`}
                    />
                    <span className="text-xs text-gray-700 dark:text-gray-300 flex-1">Cosmos DB</span>
                    {status?.resources.cosmos.deployed ? (
                      <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.cosmos.name}>
                        ✓
                      </span>
                    ) : (
                      <span className="text-xs text-red-600 dark:text-red-400">
                        ✗ Not Deployed
                      </span>
                    )}
                  </div>
                )}
                {project.infrastructure.appService && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={isTemplateSelected('appservice')}
                      onChange={() => toggleTemplateForProject(project.id, 'appservice')}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 flex-shrink-0"
                      aria-label={`Select App Service for ${project.name}`}
                    />
                    <span className="text-xs text-gray-700 dark:text-gray-300 flex-1">App Service</span>
                    {status?.resources.appService.deployed ? (
                      <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.appService.name}>
                        ✓
                      </span>
                    ) : (
                      <span className="text-xs text-red-600 dark:text-red-400">
                        ✗ Not Deployed
                      </span>
                    )}
                  </div>
                )}
                {project.infrastructure.keyVault && (
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={isTemplateSelected('keyvault')}
                      onChange={() => toggleTemplateForProject(project.id, 'keyvault')}
                      className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 flex-shrink-0"
                      aria-label={`Select Key Vault for ${project.name}`}
                    />
                    <span className="text-xs text-gray-700 dark:text-gray-300 flex-1">Key Vault</span>
                    {status?.resources.keyVault.deployed ? (
                      <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.keyVault.name}>
                        ✓
                      </span>
                    ) : (
                      <span className="text-xs text-red-600 dark:text-red-400">
                        ✗ Not Deployed
                      </span>
                    )}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Summary and Continue Button - Combined */}
      <div className="mt-6 flex items-center justify-between gap-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
        {/* Selected Templates Summary - Left side */}
        <div className="flex-1 min-w-0 flex items-center gap-2 flex-wrap">
          <span className="text-xs font-medium text-gray-700 dark:text-gray-300">Selected Templates:</span>
          {selectedTemplates.length > 0 ? (
            selectedTemplates.map(template => (
              <span
                key={template.id}
                className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
              >
                {template.name}
              </span>
            ))
          ) : (
            <span className="text-xs text-gray-500 dark:text-gray-400 italic">
              No templates selected
            </span>
          )}
        </div>

        {/* Status and Continue Button - Right side */}
        <div className="flex items-center gap-3 flex-shrink-0">
          {readyToProceed ? (
            <div className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
              <span className="text-green-600 dark:text-green-400">✓</span>
              <span>Ready</span>
            </div>
          ) : (
            <div className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
              <span className="text-yellow-600 dark:text-yellow-400">⚠</span>
              <span>Select templates</span>
            </div>
          )}
          <button
            disabled={!readyToProceed}
            onClick={() => {
              if (readyToProceed) {
                // Notify parent to show Step 2
                onProceedToStep2?.();
                
                // Scroll to Step 2 section with offset for sticky header
                // Use a small delay to ensure Step 2 is rendered
                setTimeout(() => {
                  const step2Element = document.getElementById('step-2-infrastructure-actions');
                  if (step2Element) {
                    // Use requestAnimationFrame to ensure DOM is ready
                    requestAnimationFrame(() => {
                      // Get the element's position and account for sticky header
                      const elementPosition = step2Element.getBoundingClientRect().top;
                      const offsetPosition = elementPosition + window.pageYOffset - 20; // 20px offset for spacing
                      
                      window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                      });
                      
                      // Highlight briefly
                      step2Element.classList.add('ring-2', 'ring-blue-500', 'ring-offset-2', 'rounded-lg');
                      setTimeout(() => {
                        step2Element.classList.remove('ring-2', 'ring-blue-500', 'ring-offset-2', 'rounded-lg');
                      }, 2000);
                    });
                  }
                }, 100);
              }
            }}
            className={`px-6 py-2 text-sm font-medium rounded-lg transition-colors ${
              readyToProceed
                ? 'bg-blue-600 dark:bg-blue-500 text-white hover:bg-blue-700 dark:hover:bg-blue-600'
                : 'bg-gray-300 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed'
            }`}
            title={!readyToProceed ? (infrastructureLoading || loadingStatus ? 'Please wait for infrastructure status to finish loading...' : 'Select at least one infrastructure template to continue') : 'Continue to Step 2: Infrastructure Actions'}
          >
            Continue to Step 2 →
          </button>
        </div>
      </div>
    </div>
  );
}

export default ProjectDeploymentPlanner;

