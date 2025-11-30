import type { ProjectInfo } from './ProjectDeploymentPlanner';
import { ProjectDeploymentCard, ProjectDeploymentHeader } from './components';
import { useProjectDeployment } from './hooks/useProjectDeployment';

export type { ProjectPipeline } from './types';

interface ProjectDeploymentProps {
  environment: string;
  projects: ProjectInfo[];
  hasDeployedInfrastructure: boolean;
}

function ProjectDeployment({
  environment,
  projects,
  hasDeployedInfrastructure,
}: ProjectDeploymentProps) {
  const {
    selectedProjects,
    projectPipelines,
    deploying,
    workflowRuns,
    workflowLogs,
    showLogs,
    availableWorkflows,
    logsEndRefs,
    toggleProjectSelection,
    updatePipeline,
    handleDeployProjects,
    setShowLogs,
  } = useProjectDeployment({ environment, projects });

  return (
    <div className="mb-8">
      <ProjectDeploymentHeader
        hasDeployedInfrastructure={hasDeployedInfrastructure}
        deploying={deploying}
        selectedCount={selectedProjects.size}
        onDeploy={handleDeployProjects}
      />

      {!hasDeployedInfrastructure && (
        <div className="mb-4 p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg">
          <p className="text-sm text-yellow-800 dark:text-yellow-200">
            ⚠️ Please deploy infrastructure first before deploying projects.
          </p>
        </div>
      )}

      <div className="space-y-4">
        {projects.map((project) => (
          <ProjectDeploymentCard
            key={project.id}
            project={project}
            pipeline={projectPipelines[project.id]}
            run={workflowRuns[project.id]}
            logs={workflowLogs[project.id] || []}
            showLog={showLogs[project.id]}
            isSelected={selectedProjects.has(project.id)}
            availableWorkflows={availableWorkflows}
            onToggleSelection={() => toggleProjectSelection(project.id)}
            onUpdatePipeline={(workflowFile) => updatePipeline(project.id, workflowFile)}
            onToggleLogs={() => setShowLogs(prev => ({ ...prev, [project.id]: !prev[project.id] }))}
            onRefSet={(el) => { logsEndRefs.current[project.id] = el; }}
          />
        ))}
      </div>
    </div>
  );
}

export default ProjectDeployment;
