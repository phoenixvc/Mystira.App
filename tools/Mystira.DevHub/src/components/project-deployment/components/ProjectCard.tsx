import type { ProjectInfo } from '../../../types';
import type { DeploymentStatus } from '../types';
import { getProjectTypeIcon, getProjectTypeColor } from '../utils';
import { ResourceCheckbox } from './ResourceCheckbox';

interface ProjectCardProps {
  project: ProjectInfo;
  status?: DeploymentStatus;
  onTemplateToggle: (projectId: string, templateId: string) => void;
  isTemplateSelected: (templateId: string) => boolean;
}

export function ProjectCard({
  project,
  status,
  onTemplateToggle,
  isTemplateSelected,
}: ProjectCardProps) {
  const isFullyDeployed = status?.allRequiredDeployed || false;
  const hasAnyDeployed = status && Object.values(status.resources).some((r: { deployed: boolean }) => r.deployed);

  const getDeploymentBadge = () => {
    if (!status) return null;

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
    
    if (deployedResources.length === 0 && missingResources.length > 0) {
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
  };

  return (
    <div
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
            {getDeploymentBadge()}
          </div>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{project.description}</p>
        </div>
      </div>
      
      <div className="flex flex-col gap-2 mt-auto">
        {project.infrastructure.storage && (
          <ResourceCheckbox
            label="Storage"
            checked={isTemplateSelected('storage')}
            onChange={() => onTemplateToggle(project.id, 'storage')}
            status={status?.resources.storage}
            projectName={project.name}
          />
        )}
        {project.infrastructure.cosmos && (
          <ResourceCheckbox
            label="Cosmos DB"
            checked={isTemplateSelected('cosmos')}
            onChange={() => onTemplateToggle(project.id, 'cosmos')}
            status={status?.resources.cosmos}
            projectName={project.name}
          />
        )}
        {project.infrastructure.appService && (
          <ResourceCheckbox
            label="App Service"
            checked={isTemplateSelected('appservice')}
            onChange={() => onTemplateToggle(project.id, 'appservice')}
            status={status?.resources.appService}
            projectName={project.name}
          />
        )}
        {project.infrastructure.keyVault && (
          <ResourceCheckbox
            label="Key Vault"
            checked={isTemplateSelected('keyvault')}
            onChange={() => onTemplateToggle(project.id, 'keyvault')}
            status={status?.resources.keyVault}
            projectName={project.name}
          />
        )}
      </div>
    </div>
  );
}

