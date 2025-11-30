import type { ProjectInfo } from './ProjectDeploymentPlanner';
import { ProjectDeploymentCard, ProjectDeploymentHeader } from './components';
import { useProjectDeployment, type DeploymentError, type WorkflowDiscoveryStatus } from './hooks/useProjectDeployment';

export type { ProjectPipeline } from './types';

interface ProjectDeploymentProps {
  environment: string;
  projects: ProjectInfo[];
  hasDeployedInfrastructure: boolean;
}

// Confirmation Dialog Component
function ConfirmationDialog({
  projectCount,
  onConfirm,
  onCancel,
}: {
  projectCount: number;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
    >
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-2xl max-w-md w-full mx-4 p-6">
        <h3
          id="confirm-dialog-title"
          className="text-lg font-semibold text-gray-900 dark:text-white mb-2"
        >
          Deploy Multiple Projects?
        </h3>
        <p className="text-gray-600 dark:text-gray-300 mb-6">
          You are about to deploy <strong>{projectCount} projects</strong> simultaneously.
          This will trigger {projectCount} GitHub Actions workflows.
        </p>
        <div className="flex gap-3 justify-end">
          <button
            onClick={onCancel}
            className="px-4 py-2 text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-lg font-medium transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-medium transition-colors flex items-center gap-2"
          >
            <span aria-hidden="true">🚀</span>
            Deploy All
          </button>
        </div>
      </div>
    </div>
  );
}

// Error Banner Component
function ErrorBanner({
  errors,
  onDismiss,
  onRetry,
  hasRetryable,
}: {
  errors: DeploymentError[];
  onDismiss: (index: number) => void;
  onRetry: () => void;
  hasRetryable: boolean;
}) {
  if (errors.length === 0) return null;

  return (
    <div
      role="alert"
      aria-live="assertive"
      className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg"
    >
      <div className="flex items-start justify-between mb-2">
        <h4 className="font-medium text-red-800 dark:text-red-200 flex items-center gap-2">
          <span aria-hidden="true">⚠️</span>
          Deployment Errors ({errors.length})
        </h4>
        {hasRetryable && (
          <button
            onClick={onRetry}
            className="text-sm px-3 py-1 bg-red-600 hover:bg-red-700 text-white rounded-md font-medium transition-colors"
          >
            Retry Failed
          </button>
        )}
      </div>
      <ul className="space-y-2">
        {errors.map((error, index) => (
          <li
            key={index}
            className="flex items-start justify-between text-sm text-red-700 dark:text-red-300"
          >
            <span>{error.message}</span>
            <button
              onClick={() => onDismiss(index)}
              className="ml-2 text-red-400 hover:text-red-600 flex-shrink-0"
              aria-label="Dismiss error"
            >
              ✕
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

// Workflow Discovery Status Component
function WorkflowDiscoveryBanner({
  status,
  onRefresh,
}: {
  status: WorkflowDiscoveryStatus;
  onRefresh: () => void;
}) {
  if (status.loading) {
    return (
      <div className="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg flex items-center gap-2">
        <svg
          className="animate-spin h-4 w-4 text-blue-600 dark:text-blue-400"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
        <span className="text-sm text-blue-700 dark:text-blue-300">Discovering available workflows...</span>
      </div>
    );
  }

  if (status.usingFallback) {
    return (
      <div
        role="status"
        className="mb-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg"
      >
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-2">
            <span className="text-yellow-500 flex-shrink-0" aria-hidden="true">⚠️</span>
            <div>
              <p className="text-sm text-yellow-800 dark:text-yellow-200 font-medium">
                Using default workflow list
              </p>
              <p className="text-xs text-yellow-700 dark:text-yellow-300 mt-0.5">
                {status.error || 'Could not discover workflows from GitHub.'}
              </p>
            </div>
          </div>
          <button
            onClick={onRefresh}
            className="text-xs px-2 py-1 bg-yellow-100 dark:bg-yellow-800 hover:bg-yellow-200 dark:hover:bg-yellow-700 text-yellow-800 dark:text-yellow-200 rounded transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return null;
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
    setShowLogs,

    // New UX states
    deploymentErrors,
    validationError,
    workflowDiscoveryStatus,
    showConfirmDialog,
    failedProjects,

    // New UX actions
    requestDeploy,
    confirmDeploy,
    cancelDeploy,
    dismissError,
    retryFailedProjects,
    refreshWorkflows,
  } = useProjectDeployment({ environment, projects });

  const hasRetryableErrors = deploymentErrors.some(e => e.retryable) && failedProjects.size > 0;

  return (
    <div className="mb-8">
      {/* Confirmation Dialog */}
      {showConfirmDialog && (
        <ConfirmationDialog
          projectCount={selectedProjects.size}
          onConfirm={confirmDeploy}
          onCancel={cancelDeploy}
        />
      )}

      <ProjectDeploymentHeader
        hasDeployedInfrastructure={hasDeployedInfrastructure}
        deploying={deploying}
        selectedCount={selectedProjects.size}
        onDeploy={requestDeploy}
        validationError={validationError}
      />

      {/* Workflow Discovery Status */}
      <WorkflowDiscoveryBanner
        status={workflowDiscoveryStatus}
        onRefresh={refreshWorkflows}
      />

      {/* Deployment Errors */}
      <ErrorBanner
        errors={deploymentErrors}
        onDismiss={dismissError}
        onRetry={retryFailedProjects}
        hasRetryable={hasRetryableErrors}
      />

      {!hasDeployedInfrastructure && (
        <div
          role="alert"
          className="mb-4 p-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg flex items-start gap-3"
        >
          <span className="text-yellow-500 flex-shrink-0 text-xl" aria-hidden="true">⚠️</span>
          <div>
            <p className="font-medium text-yellow-800 dark:text-yellow-200">
              Infrastructure Not Deployed
            </p>
            <p className="text-sm text-yellow-700 dark:text-yellow-300 mt-1">
              Please complete Steps 1 and 2 to deploy infrastructure before deploying projects.
            </p>
          </div>
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
            isFailed={failedProjects.has(project.id)}
            availableWorkflows={availableWorkflows}
            onToggleSelection={() => toggleProjectSelection(project.id)}
            onUpdatePipeline={(workflowFile) => updatePipeline(project.id, workflowFile)}
            onToggleLogs={() => setShowLogs(prev => ({ ...prev, [project.id]: !prev[project.id] }))}
            onRefSet={(el) => { logsEndRefs.current[project.id] = el; }}
          />
        ))}
      </div>

      {/* Summary when deploying */}
      {deploying && (
        <div className="mt-4 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <div className="flex items-center gap-3">
            <svg
              className="animate-spin h-5 w-5 text-blue-600 dark:text-blue-400"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
            <div>
              <p className="font-medium text-blue-800 dark:text-blue-200">
                Deploying {selectedProjects.size} project{selectedProjects.size > 1 ? 's' : ''}...
              </p>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                Workflows are being dispatched. Check the logs for real-time progress.
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default ProjectDeployment;
