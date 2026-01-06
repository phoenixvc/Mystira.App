import { DeploymentHistory } from '../../../project-deployment';
import { useCliBuild } from '../../hooks';
import { CliBuildLogsViewer } from '../CliBuildLogsViewer';

interface InfrastructureHistoryTabProps {
  deployments: any[];
  deploymentsLoading: boolean;
  deploymentsError: string | null;
  onFetchDeployments: (force?: boolean) => void;
}

export default function InfrastructureHistoryTab({
  deployments,
  deploymentsLoading,
  deploymentsError,
  onFetchDeployments,
}: InfrastructureHistoryTabProps) {
  const { isBuilding, logs, showLogs, setShowLogs, build } = useCliBuild();

  const handleBuildCli = async () => {
    await build();
    setTimeout(() => {
      onFetchDeployments(true);
    }, 1000);
  };

  return (
    <div>
      {deploymentsLoading && (
        <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-8 text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:bg-blue-400 mb-3"></div>
          <p className="text-blue-800 dark:text-blue-200">Loading deployment history...</p>
        </div>
      )}

      {deploymentsError && (
        <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-4">
          <h3 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">❌ Failed to Load Deployments</h3>
          <p className="text-red-800 dark:text-red-200 mb-3 whitespace-pre-wrap">{deploymentsError}</p>
          <div className="flex gap-3">
            <button
              onClick={() => onFetchDeployments(true)}
              className="px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
            >
              Retry
            </button>
            {(deploymentsError.includes('Could not find Mystira.DevHub.CLI') ||
              deploymentsError.includes('Program not found') ||
              deploymentsError.includes('Failed to spawn process')) && (
              <button
                onClick={handleBuildCli}
                disabled={isBuilding}
                className="px-4 py-2 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isBuilding ? (
                  <>
                    <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></span>
                    Building...
                  </>
                ) : (
                  '🔨 Rebuild CLI'
                )}
              </button>
            )}
          </div>
        </div>
      )}

      {!deploymentsLoading && !deploymentsError && <DeploymentHistory events={deployments} />}

      {showLogs && (
        <CliBuildLogsViewer
          logs={logs}
          isBuilding={isBuilding}
          showLogs={showLogs}
          onClose={() => setShowLogs(false)}
        />
      )}
    </div>
  );
}

