import type { ProjectInfo } from '../../ProjectDeploymentPlanner';
import type { ProjectPipeline, WorkflowRun } from '../types';
import { getProjectTypeIcon, getStatusColor, getStatusIcon } from '../utils';
import { WorkflowLogsViewer } from './WorkflowLogsViewer';

interface ProjectDeploymentCardProps {
  project: ProjectInfo;
  pipeline: ProjectPipeline | undefined;
  run: WorkflowRun | undefined;
  logs: string[];
  showLog: boolean;
  isSelected: boolean;
  isFailed?: boolean;
  availableWorkflows: string[];
  onToggleSelection: () => void;
  onUpdatePipeline: (workflowFile: string) => void;
  onToggleLogs: () => void;
  onRefSet: (el: HTMLDivElement | null) => void;
}

// Helper to get accessible status text
function getStatusText(status: string, conclusion: string | null): string {
  if (status === 'completed') {
    return conclusion === 'success' ? 'Deployment succeeded' : 'Deployment failed';
  }
  if (status === 'in_progress') return 'Deployment in progress';
  if (status === 'queued') return 'Deployment queued';
  return `Status: ${status}`;
}

export function ProjectDeploymentCard({
  project,
  pipeline,
  run,
  logs,
  showLog,
  isSelected,
  isFailed = false,
  availableWorkflows,
  onToggleSelection,
  onUpdatePipeline,
  onToggleLogs,
  onRefSet,
}: ProjectDeploymentCardProps) {
  const hasWorkflowSelected = !!pipeline?.workflowFile;
  const statusText = run ? getStatusText(run.status, run.conclusion) : null;

  return (
    <div
      className={`border-2 rounded-lg p-4 transition-all duration-200 ${
        isFailed
          ? 'border-red-400 dark:border-red-600 bg-red-50 dark:bg-red-900/20 ring-2 ring-red-200 dark:ring-red-800'
          : isSelected
          ? 'border-green-500 dark:border-green-600 bg-green-50 dark:bg-green-900/20 ring-2 ring-green-200 dark:ring-green-800'
          : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-600'
      }`}
      role="article"
      aria-label={`${project.name} deployment card`}
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-2xl">
            {getProjectTypeIcon(project.type)}
          </div>
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1 flex-wrap">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={isSelected}
                onChange={onToggleSelection}
                className="w-4 h-4 text-green-600 border-gray-300 rounded focus:ring-green-500 focus:ring-2 focus:ring-offset-2 dark:focus:ring-offset-gray-800"
                aria-describedby={`project-desc-${project.id}`}
              />
              <span className="font-semibold text-gray-900 dark:text-white">{project.name}</span>
            </label>

            {/* Status badge with accessible text */}
            {run && (
              <span
                className={`inline-flex items-center gap-1.5 text-sm font-medium px-2 py-0.5 rounded-full ${getStatusColor(run.status, run.conclusion)}`}
                role="status"
                aria-label={statusText || undefined}
              >
                <span aria-hidden="true">{getStatusIcon(run.status, run.conclusion)}</span>
                <span className="capitalize">{run.status}</span>
                {run.conclusion && run.status === 'completed' && (
                  <span className="text-xs opacity-75">({run.conclusion})</span>
                )}
              </span>
            )}

            {/* Failed indicator */}
            {isFailed && !run && (
              <span
                className="inline-flex items-center gap-1.5 text-sm font-medium px-2 py-0.5 rounded-full bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300"
                role="status"
              >
                <span aria-hidden="true">⚠️</span>
                <span>Failed to dispatch</span>
              </span>
            )}
          </div>

          <p
            id={`project-desc-${project.id}`}
            className="text-sm text-gray-600 dark:text-gray-400 mb-3"
          >
            {project.description}
          </p>

          {/* Workflow selector with improved accessibility */}
          <div className="mb-3">
            <label
              htmlFor={`workflow-select-${project.id}`}
              className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5 block"
            >
              GitHub Workflow
              {!hasWorkflowSelected && isSelected && (
                <span className="text-red-500 ml-1" aria-label="required">*</span>
              )}
            </label>
            <div className="relative">
              <select
                id={`workflow-select-${project.id}`}
                value={pipeline?.workflowFile || ''}
                onChange={(e) => onUpdatePipeline(e.target.value)}
                className={`w-full px-3 py-2 text-sm border rounded-md shadow-sm transition-colors
                  focus:ring-2 focus:ring-green-500 focus:border-green-500 focus:outline-none
                  dark:bg-gray-700 dark:text-white
                  ${!hasWorkflowSelected && isSelected
                    ? 'border-red-300 dark:border-red-600 bg-red-50 dark:bg-red-900/20'
                    : 'border-gray-300 dark:border-gray-600'
                  }
                `}
                aria-describedby={`workflow-help-${project.id}`}
                aria-invalid={!hasWorkflowSelected && isSelected}
              >
                <option value="">Select a workflow...</option>
                {availableWorkflows.map(workflow => (
                  <option key={workflow} value={workflow}>
                    {workflow}
                  </option>
                ))}
              </select>
            </div>
            <p
              id={`workflow-help-${project.id}`}
              className="mt-1 text-xs text-gray-500 dark:text-gray-400"
            >
              Choose the GitHub Actions workflow to deploy this project
            </p>
            {!hasWorkflowSelected && isSelected && (
              <p className="mt-1 text-xs text-red-600 dark:text-red-400" role="alert">
                Please select a workflow to deploy this project
              </p>
            )}
          </div>

          {/* GitHub link */}
          {run && run.html_url && (
            <div className="mb-3">
              <a
                href={run.html_url}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-xs text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 hover:underline focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-1 rounded"
              >
                <span aria-hidden="true">🔗</span>
                <span>View workflow on GitHub</span>
                <span className="sr-only">(opens in new tab)</span>
              </a>
            </div>
          )}

          <WorkflowLogsViewer
            projectId={project.id}
            logs={logs}
            run={run}
            showLog={showLog}
            onToggle={onToggleLogs}
            logsEndRef={null}
            onRefSet={onRefSet}
          />
        </div>
      </div>
    </div>
  );
}
