import { useEffect, useState } from 'react';
import type { ProjectInfo, ResourceGroupConvention, TemplateConfig } from '../../types';
import { DEFAULT_PROJECTS } from '../../types';
import {
    ProjectCard,
    ProjectDeploymentPlannerHeader,
    ProjectDeploymentSummary,
} from './components';
import { useDeploymentStatus } from './hooks/useDeploymentStatus';
export type { DeploymentStatus } from './types';
export type { ProjectInfo };

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

  const {
    deploymentStatus,
    loadingStatus,
    lastRefreshTime,
    error,
    loadDeploymentStatus,
  } = useDeploymentStatus({
    environment,
    resourceGroupConfig,
    projects,
  });

  useEffect(() => {
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

    onReadyToProceed?.(true);
  }, [templates, onReadyToProceed, infrastructureLoading, loadingStatus]);

  const toggleTemplateForProject = (_projectId: string, templateId: string) => {
    const updatedTemplates = templates.map(t => {
      if (t.id === templateId) {
        return { ...t, selected: !t.selected };
      }
      return t;
    });

    onTemplatesChange(updatedTemplates);
  };

  const isTemplateSelected = (templateId: string) => {
    const template = templates.find(t => t.id === templateId);
    return template?.selected || false;
  };

  const handleSelectAll = () => {
    const updatedTemplates = templates.map(t => ({ ...t, selected: true }));
    onTemplatesChange(updatedTemplates);
  };

  const handleDeselectAll = () => {
    const updatedTemplates = templates.map(t => ({ ...t, selected: false }));
    onTemplatesChange(updatedTemplates);
  };

  const selectedTemplates = templates.filter(t => t.selected);
  const readyToProceed = selectedTemplates.length > 0 && !infrastructureLoading && !loadingStatus;

  return (
    <div className="mb-8">
      <ProjectDeploymentPlannerHeader
        lastRefreshTime={lastRefreshTime}
        loadingStatus={loadingStatus}
        onRefresh={loadDeploymentStatus}
        selectedCount={selectedTemplates.length}
        totalCount={templates.length}
        onSelectAll={handleSelectAll}
        onDeselectAll={handleDeselectAll}
      />

      {error && (
        <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
          <p className="text-sm text-red-700 dark:text-red-300">{error}</p>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {projects.map((project) => (
          <ProjectCard
            key={project.id}
            project={project}
            status={deploymentStatus[project.id]}
            onTemplateToggle={toggleTemplateForProject}
            isTemplateSelected={isTemplateSelected}
          />
        ))}
      </div>

      <ProjectDeploymentSummary
        selectedTemplates={selectedTemplates}
        readyToProceed={readyToProceed}
        infrastructureLoading={infrastructureLoading}
        loadingStatus={loadingStatus}
        onProceedToStep2={onProceedToStep2}
      />
    </div>
  );
}

export default ProjectDeploymentPlanner;
