import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import type { ProjectInfo, ResourceGroupConvention, TemplateConfig } from '../types';
import { DEFAULT_PROJECTS } from '../types';

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
}

interface ProjectDeploymentPlannerProps {
  environment: string;
  resourceGroupConfig: ResourceGroupConvention;
  templates: TemplateConfig[];
  onTemplatesChange: (templates: TemplateConfig[]) => void;
  onEditTemplate: (template: TemplateConfig) => void;
  region?: string;
  projectName?: string;
}

function ProjectDeploymentPlanner({
  environment,
  templates,
  onTemplatesChange,
}: ProjectDeploymentPlannerProps) {
  const [projects] = useState<ProjectInfo[]>(DEFAULT_PROJECTS);

  const [deploymentStatus, setDeploymentStatus] = useState<Record<string, DeploymentStatus>>({});
  const [loadingStatus, setLoadingStatus] = useState(false);

  useEffect(() => {
    loadDeploymentStatus();
  }, [environment]);

  const loadDeploymentStatus = async () => {
    setLoadingStatus(true);
    try {
      const response = await invoke<any>('get_azure_resources', {});
      if (response.success && response.result) {
        const resources = Array.isArray(response.result) ? response.result : [];
        const status: Record<string, DeploymentStatus> = {};

        projects.forEach(project => {
          const projectStatus: DeploymentStatus = {
            projectId: project.id,
            resources: {
              storage: { deployed: false },
              cosmos: { deployed: false },
              appService: { deployed: false },
              keyVault: { deployed: false },
            },
          };

          resources.forEach((resource: any) => {
            const resourceType = resource.type || '';
            const resourceName = resource.name || '';

            if (resourceType.includes('Microsoft.Storage/storageAccounts') && project.infrastructure.storage) {
              projectStatus.resources.storage = { deployed: true, name: resourceName };
            }
            if (resourceType.includes('Microsoft.DocumentDB/databaseAccounts') && project.infrastructure.cosmos) {
              projectStatus.resources.cosmos = { deployed: true, name: resourceName };
            }
            if (resourceType.includes('Microsoft.Web/sites') && project.infrastructure.appService) {
              projectStatus.resources.appService = { deployed: true, name: resourceName };
            }
            if (resourceType.includes('Microsoft.KeyVault/vaults') && project.infrastructure.keyVault) {
              projectStatus.resources.keyVault = { deployed: true, name: resourceName };
            }
          });

          status[project.id] = projectStatus;
        });

        setDeploymentStatus(status);
      }
    } catch (error) {
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
        <button
          onClick={loadDeploymentStatus}
          disabled={loadingStatus}
          className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded disabled:opacity-50"
        >
          {loadingStatus ? '🔄 Loading...' : '🔄 Refresh Status'}
        </button>
      </div>

      <div className="space-y-4">
        {projects.map((project) => {
          const status = deploymentStatus[project.id];
          const hasDeployedResources = status && Object.values(status.resources).some(r => r.deployed);

          return (
            <div
              key={project.id}
              className={`border-2 rounded-lg p-4 transition-all ${
                hasDeployedResources
                  ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0">
                  <div className={`w-12 h-12 rounded-lg flex items-center justify-center text-2xl ${getProjectTypeColor(project.type)}`}>
                    {getProjectTypeIcon(project.type)}
                  </div>
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="font-semibold text-gray-900 dark:text-white">{project.name}</h4>
                    <span className={`px-2 py-0.5 text-xs rounded ${getProjectTypeColor(project.type)}`}>
                      {project.type}
                    </span>
                    {hasDeployedResources && (
                      <span className="px-2 py-0.5 text-xs rounded bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-300">
                        ✓ Deployed
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">{project.description}</p>

                  {/* Infrastructure Requirements */}
                  <div className="space-y-2">
                    <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Required Infrastructure:
                    </div>
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                      {project.infrastructure.storage && (
                        <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <input
                            type="checkbox"
                            checked={isTemplateSelected('storage')}
                            onChange={() => toggleTemplateForProject(project.id, 'storage')}
                            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                            aria-label={`Select storage for ${project.name}`}
                          />
                          <span className="text-xs text-gray-700 dark:text-gray-300">Storage</span>
                          {status?.resources.storage.deployed && (
                            <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.storage.name}>
                              ✓
                            </span>
                          )}
                        </div>
                      )}
                      {project.infrastructure.cosmos && (
                        <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <input
                            type="checkbox"
                            checked={isTemplateSelected('cosmos')}
                            onChange={() => toggleTemplateForProject(project.id, 'cosmos')}
                            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                            aria-label={`Select Cosmos DB for ${project.name}`}
                          />
                          <span className="text-xs text-gray-700 dark:text-gray-300">Cosmos DB</span>
                          {status?.resources.cosmos.deployed && (
                            <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.cosmos.name}>
                              ✓
                            </span>
                          )}
                        </div>
                      )}
                      {project.infrastructure.appService && (
                        <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <input
                            type="checkbox"
                            checked={isTemplateSelected('appservice')}
                            onChange={() => toggleTemplateForProject(project.id, 'appservice')}
                            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                            aria-label={`Select App Service for ${project.name}`}
                          />
                          <span className="text-xs text-gray-700 dark:text-gray-300">App Service</span>
                          {status?.resources.appService.deployed && (
                            <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.appService.name}>
                              ✓
                            </span>
                          )}
                        </div>
                      )}
                      {project.infrastructure.keyVault && (
                        <div className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <input
                            type="checkbox"
                            checked={isTemplateSelected('keyvault')}
                            onChange={() => toggleTemplateForProject(project.id, 'keyvault')}
                            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                            aria-label={`Select Key Vault for ${project.name}`}
                          />
                          <span className="text-xs text-gray-700 dark:text-gray-300">Key Vault</span>
                          {status?.resources.keyVault.deployed && (
                            <span className="text-xs text-green-600 dark:text-green-400" title={status.resources.keyVault.name}>
                              ✓
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Summary */}
      <div className="mt-6 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
        <div className="text-sm font-medium text-blue-900 dark:text-blue-200 mb-2">
          Selected Templates Summary
        </div>
        <div className="flex flex-wrap gap-2">
          {templates.filter(t => t.selected).map(template => (
            <span
              key={template.id}
              className="px-3 py-1 text-xs bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
            >
              {template.name}
            </span>
          ))}
          {templates.filter(t => t.selected).length === 0 && (
            <span className="text-xs text-gray-500 dark:text-gray-400 italic">
              No templates selected. Select infrastructure above to proceed.
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

export default ProjectDeploymentPlanner;

