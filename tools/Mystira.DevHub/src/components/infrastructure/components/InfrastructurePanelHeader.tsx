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

const environmentColors: Record<string, { bg: string; text: string; border: string; warning?: boolean }> = {
  dev: {
    bg: 'bg-green-100 dark:bg-green-900/30',
    text: 'text-green-700 dark:text-green-300',
    border: 'border-green-200 dark:border-green-800',
  },
  staging: {
    bg: 'bg-yellow-100 dark:bg-yellow-900/30',
    text: 'text-yellow-700 dark:text-yellow-300',
    border: 'border-yellow-200 dark:border-yellow-800',
  },
  prod: {
    bg: 'bg-red-100 dark:bg-red-900/30',
    text: 'text-red-700 dark:text-red-300',
    border: 'border-red-200 dark:border-red-800',
    warning: true,
  },
};

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

  const envColors = environmentColors[environment] || environmentColors.dev;
  const workflowSuccess = workflowStatus?.conclusion === 'success';

  return (
    <div className="mb-4">
      <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4">
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <h2 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">
            Infrastructure Control Panel
          </h2>
          <div className="flex items-center gap-2 flex-wrap">
            <label className="text-sm text-gray-600 dark:text-gray-400">Environment:</label>
            <div className="relative">
              <select
                value={environment}
                aria-label="Select environment"
                onChange={handleEnvironmentChange}
                className={`px-3 py-1.5 text-sm border rounded-md shadow-sm appearance-none pr-8 font-medium
                  focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900
                  transition-all duration-200 ${envColors.bg} ${envColors.text} ${envColors.border}`}
              >
                <option value="dev">dev</option>
                <option value="staging">staging</option>
                <option value="prod">prod</option>
              </select>
              <div className="absolute inset-y-0 right-0 flex items-center pr-2 pointer-events-none">
                <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </div>
            {envColors.warning && (
              <span className="px-2 py-1 text-xs bg-red-100 dark:bg-red-900/50 text-red-700 dark:text-red-300 rounded-full font-medium animate-pulse">
                ⚠️ Production
              </span>
            )}
            <button
              onClick={onShowResourceGroupConfig}
              className="px-4 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-lg text-sm font-medium transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
              title="Configure resource group naming conventions"
            >
              ⚙️ Resource Groups
            </button>
          </div>
        </div>
        <div className="flex flex-row sm:flex-col items-center sm:items-end gap-2 sm:gap-3">
          {/* Workflow Status */}
          {workflowStatus?.updatedAt && (
            <div className="flex items-center gap-2" title={`Last workflow: ${new Date(workflowStatus.updatedAt).toLocaleString()} - ${workflowStatus.conclusion || 'unknown'}`}>
              <div className="text-xs text-gray-500 dark:text-gray-400 hidden sm:block">Workflow</div>
              <div className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg font-mono font-semibold text-sm ${
                workflowSuccess
                  ? 'bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400'
                  : 'bg-amber-900/20 dark:bg-amber-900/30 text-amber-600 dark:text-amber-400'
              }`}>
                {workflowSuccess ? (
                  <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
                ) : (
                  <span className="w-2 h-2 rounded-full bg-amber-500" />
                )}
                {formatTimeSince(new Date(workflowStatus.updatedAt).getTime()) || 'Unknown'}
              </div>
            </div>
          )}

          {/* CLI Build Status */}
          <div className="flex items-center gap-2">
            <div className="text-xs text-gray-500 dark:text-gray-400 hidden sm:block">CLI</div>
            {cliBuildTime ? (
              <div className="flex items-center gap-2">
                <div
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400 font-mono font-semibold text-sm"
                  title={`Last CLI build: ${new Date(cliBuildTime).toLocaleString()}`}
                >
                  <span className="w-2 h-2 rounded-full bg-green-500" />
                  {formatTimeSince(cliBuildTime) || 'Unknown'}
                </div>
                <button
                  onClick={() => {
                    onShowCliBuildLogs(true);
                    onBuildCli();
                  }}
                  disabled={isBuildingCli}
                  className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
                  title="Rebuild the CLI executable"
                >
                  {isBuildingCli ? (
                    <>
                      <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                      <span className="hidden sm:inline">Building...</span>
                    </>
                  ) : (
                    <>🔨 <span className="hidden sm:inline">Rebuild</span></>
                  )}
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-red-900/20 dark:bg-red-900/30 text-red-600 dark:text-red-400 font-mono font-semibold text-sm">
                  <span className="w-2 h-2 rounded-full bg-red-500" />
                  Not Built
                </div>
                <button
                  onClick={() => {
                    onShowCliBuildLogs(true);
                    onBuildCli();
                  }}
                  disabled={isBuildingCli}
                  className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900"
                  title="Build the CLI executable"
                >
                  {isBuildingCli ? (
                    <>
                      <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                      <span className="hidden sm:inline">Building...</span>
                    </>
                  ) : (
                    <>🔨 <span className="hidden sm:inline">Build CLI</span></>
                  )}
                </button>
              </div>
            )}
          </div>
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

