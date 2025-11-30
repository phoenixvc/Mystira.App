import type { WorkflowStatus } from '../../../types';

interface WorkflowStatusDisplayProps {
  workflowStatus: WorkflowStatus | null;
}

export function WorkflowStatusDisplay({ workflowStatus }: WorkflowStatusDisplayProps) {
  if (!workflowStatus) return null;

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
        Workflow Status
      </h3>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
        <div>
          <div className="text-sm text-gray-500 dark:text-gray-400">Status</div>
          <div className="text-lg font-semibold text-gray-900 dark:text-white">
            {workflowStatus.status || 'Unknown'}
          </div>
        </div>
        <div>
          <div className="text-sm text-gray-500 dark:text-gray-400">Conclusion</div>
          <div className="text-lg font-semibold text-gray-900 dark:text-white">
            {workflowStatus.conclusion || 'N/A'}
          </div>
        </div>
        <div>
          <div className="text-sm text-gray-500 dark:text-gray-400">Workflow</div>
          <div className="text-lg font-semibold text-gray-900 dark:text-white">
            {workflowStatus.workflowName || 'N/A'}
          </div>
        </div>
        <div>
          <div className="text-sm text-gray-500 dark:text-gray-400">Updated</div>
          <div className="text-lg font-semibold text-gray-900 dark:text-white">
            {workflowStatus.updatedAt
              ? new Date(workflowStatus.updatedAt).toLocaleTimeString()
              : 'N/A'}
          </div>
        </div>
      </div>
    </div>
  );
}

