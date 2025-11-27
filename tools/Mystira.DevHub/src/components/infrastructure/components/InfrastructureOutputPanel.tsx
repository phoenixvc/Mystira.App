import type { CommandResponse, WorkflowStatus } from '../../../types';
import { ErrorDisplay, SuccessDisplay } from '../../ui';

interface InfrastructureOutputPanelProps {
  loading: boolean;
  response: CommandResponse | null;
  workflowStatus: WorkflowStatus | null;
  onClose: () => void;
  onRefreshWorkflow: () => void;
}

export function InfrastructureOutputPanel({
  loading,
  response,
  workflowStatus,
  onClose,
  onRefreshWorkflow,
}: InfrastructureOutputPanelProps) {
  return (
    <div className="w-80 border-l border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 flex flex-col">
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-200 dark:border-gray-700">
        <span className="text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase">Output</span>
        <button
          onClick={onClose}
          className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
        >
          ✕
        </button>
      </div>
      <div className="flex-1 overflow-auto p-3 text-xs">
        {loading && (
          <div className="flex items-center gap-2 text-blue-600">
            <span className="animate-spin">⟳</span>
            <span>Executing...</span>
          </div>
        )}
        {response && (
          response.success ? (
            <SuccessDisplay message={response.message || 'Success'} details={response.result as Record<string, unknown> | null} />
          ) : (
            <ErrorDisplay error={response.error || 'Error'} details={response.result as Record<string, unknown> | null} />
          )
        )}
        {!loading && !response && (
          <div className="text-gray-500">No output yet.</div>
        )}

        {workflowStatus && (
          <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-600">
            <div className="text-[10px] font-semibold text-gray-500 uppercase mb-2">Workflow</div>
            <div className="grid grid-cols-2 gap-2 text-xs">
              <div>
                <div className="text-gray-400">Status</div>
                <div className="font-medium text-gray-900 dark:text-white">{workflowStatus.status || 'Unknown'}</div>
              </div>
              <div>
                <div className="text-gray-400">Conclusion</div>
                <div className="font-medium text-gray-900 dark:text-white">{workflowStatus.conclusion || 'N/A'}</div>
              </div>
            </div>
            <div className="flex gap-2 mt-2">
              {workflowStatus.htmlUrl && (
                <a
                  href={workflowStatus.htmlUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="px-2 py-1 bg-blue-600 text-white rounded text-[10px] hover:bg-blue-700"
                >
                  GitHub →
                </a>
              )}
              <button
                onClick={onRefreshWorkflow}
                className="px-2 py-1 bg-gray-600 text-white rounded text-[10px] hover:bg-gray-700"
              >
                Refresh
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

