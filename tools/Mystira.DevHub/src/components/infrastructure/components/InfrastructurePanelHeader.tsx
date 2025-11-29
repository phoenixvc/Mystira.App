import type { WorkflowStatus } from '../../../types';
import { formatTimeSince } from '../../services/utils/serviceUtils';
import { CliBuildLogsViewer } from './CliBuildLogsViewer';

interface InfrastructurePanelHeaderProps {
  environment: string;
  onEnvironmentChange: (env: string) => Promise<void>;
  onShowResourceGroupConfig: () => void;
  workflowStatus: WorkflowStatus | null;
  cliBuildTime: number | null;
  isBuildingCli: boolean;
  cliBuildLogs: string[];
  showCliBuildLogs: boolean;
  onShowCliBuildLogs: (show: boolean) => void;
  onBuildCli: () => void;
}

export function InfrastructurePanelHeader({
  environment,
  onEnvironmentChange,
  onShowResourceGroupConfig,
  workflowStatus,
  cliBuildTime,
  isBuildingCli,
  cliBuildLogs,
  showCliBuildLogs,
  onShowCliBuildLogs,
  onBuildCli,
}: InfrastructurePanelHeaderProps) {
  const handleEnvironmentChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newEnv = e.target.value;
    await onEnvironmentChange(newEnv);
  };

  return (
    <div className="mb-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <h2 className="text-3xl font-bold text-gray-900 dark:text-white">
            Infrastructure Control Panel
          </h2>
          <div className="flex items-center gap-2">
            <label className="text-sm text-gray-600 dark:text-gray-400">Environment:</label>
            <select
              value={environment}
              aria-label="Select environment"
              onChange={handleEnvironmentChange}
              className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
            >
              <option value="dev">dev</option>
              <option value="staging">staging</option>
              <option value="prod">prod</option>
            </select>
            <button
              onClick={onShowResourceGroupConfig}
              className="px-4 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-lg text-sm font-medium"
              title="Configure resource group naming conventions"
            >
              ⚙️ Resource Groups
            </button>
          </div>
        </div>
        <div className="flex flex-col items-end gap-2">
          {workflowStatus?.updatedAt && (
            <div className="flex flex-col items-end">
              <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last Workflow Build</div>
              <div 
                className="px-3 py-1.5 rounded-lg bg-blue-900/20 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-mono font-semibold text-sm" 
                title={`Last workflow build: ${new Date(workflowStatus.updatedAt).toLocaleString()}`}
              >
                {formatTimeSince(new Date(workflowStatus.updatedAt).getTime()) || 'Unknown'}
              </div>
            </div>
          )}
          {cliBuildTime ? (
            <div className="flex flex-col items-end">
              <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last CLI Build</div>
              <div className="flex items-center gap-2">
                <div 
                  className="px-3 py-1.5 rounded-lg bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400 font-mono font-semibold text-sm" 
                  title={`Last CLI build: ${new Date(cliBuildTime).toLocaleString()}`}
                >
                  {formatTimeSince(cliBuildTime) || 'Unknown'}
                </div>
                <button
                  onClick={() => {
                    onShowCliBuildLogs(true);
                    onBuildCli();
                  }}
                  disabled={isBuildingCli}
                  className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5"
                  title="Rebuild the CLI executable"
                >
                  {isBuildingCli ? (
                    <>
                      <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                      Building...
                    </>
                  ) : (
                    <>🔨 Rebuild</>
                  )}
                </button>
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-end">
              <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">CLI Status</div>
              <div className="flex items-center gap-2">
                <div className="px-3 py-1.5 rounded-lg bg-red-900/20 dark:bg-red-900/30 text-red-600 dark:text-red-400 font-mono font-semibold text-sm">
                  Not Built
                </div>
                <button
                  onClick={() => {
                    onShowCliBuildLogs(true);
                    onBuildCli();
                  }}
                  disabled={isBuildingCli}
                  className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5"
                  title="Build the CLI executable"
                >
                  {isBuildingCli ? (
                    <>
                      <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                      Building...
                    </>
                  ) : (
                    <>🔨 Build CLI</>
                  )}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
      
      {showCliBuildLogs && (
        <div className="mb-6">
          <CliBuildLogsViewer
            isBuilding={isBuildingCli}
            logs={cliBuildLogs}
            showLogs={showCliBuildLogs}
            onClose={() => onShowCliBuildLogs(false)}
          />
        </div>
      )}
    </div>
  );
}

