import type { ProjectInfo } from '../ProjectDeploymentPlanner';
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
  availableWorkflows: string[];
  onToggleSelection: () => void;
  onUpdatePipeline: (workflowFile: string) => void;
  onToggleLogs: () => void;
  onRefSet: (el: HTMLDivElement | null) => void;
}

export function ProjectDeploymentCard({
  project,
  pipeline,
  run,
  logs,
  showLog,
  isSelected,
  availableWorkflows,
  onToggleSelection,
  onUpdatePipeline,
  onToggleLogs,
  onRefSet,
}: ProjectDeploymentCardProps) {
  return (
    <div
      className={`border-2 rounded-lg p-4 transition-all ${
        isSelected
          ? 'border-green-500 dark:border-green-600 bg-green-50 dark:bg-green-900/20'
          : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
      }`}
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          <div className="w-12 h-12 rounded-lg bg-blue-100 dark:bg-blue-900 flex items-center justify-center text-2xl">
            {getProjectTypeIcon(project.type)}
          </div>
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <input
              type="checkbox"
              checked={isSelected}
              onChange={onToggleSelection}
              className="w-4 h-4 text-green-600 border-gray-300 rounded focus:ring-green-500"
              aria-label={`Select ${project.name} for deployment`}
            />
            <h4 className="font-semibold text-gray-900 dark:text-white">{project.name}</h4>
            {run && (
              <span className={`text-sm font-medium ${getStatusColor(run.status, run.conclusion)}`}>
                {getStatusIcon(run.status, run.conclusion)} {run.status}
              </span>
            )}
          </div>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{project.description}</p>

          <div className="mb-3">
            <label className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1 block">
              GitHub Workflow:
            </label>
            <select
              value={pipeline?.workflowFile || ''}
              onChange={(e) => onUpdatePipeline(e.target.value)}
              className="w-full px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-green-500 focus:border-green-500 dark:bg-gray-700 dark:text-white"
              aria-label={`GitHub workflow for ${project.name}`}
            >
              <option value="">Select workflow...</option>
              {availableWorkflows.map(workflow => (
                <option key={workflow} value={workflow}>
                  {workflow}
                </option>
              ))}
            </select>
          </div>

          {run && run.html_url && (
            <div className="mb-3">
              <a
                href={run.html_url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-xs text-blue-600 dark:text-blue-400 hover:underline"
              >
                🔗 View on GitHub
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

