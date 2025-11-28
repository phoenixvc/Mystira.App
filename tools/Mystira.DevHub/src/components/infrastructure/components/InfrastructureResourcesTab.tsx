import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse } from '../../../types';
import ResourceGrid from '../../ResourceGrid';
import { useCliBuild } from '../hooks';
import { CliBuildLogsViewer } from './CliBuildLogsViewer';

interface InfrastructureResourcesTabProps {
  environment: string;
  resources: any[];
  resourcesLoading: boolean;
  resourcesError: string | null;
  onFetchResources: (force?: boolean, env?: string) => void;
}

export default function InfrastructureResourcesTab({
  environment,
  resources,
  resourcesLoading,
  resourcesError,
  onFetchResources,
}: InfrastructureResourcesTabProps) {
  const { isBuilding, logs, showLogs, setShowLogs, build } = useCliBuild();

  const handleBuildCli = async () => {
    await build();
    setTimeout(() => {
      onFetchResources(true, environment);
    }, 1000);
  };

  return (
    <div>
      {resourcesLoading && (
        <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-800 rounded-lg p-8 text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:bg-blue-400 mb-3"></div>
          <p className="text-blue-800 dark:text-blue-200">Loading Azure resources...</p>
        </div>
      )}

      {resourcesError && (
        <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-4">
          <h3 className="text-lg font-semibold text-red-900 dark:text-red-300 mb-2">❌ Failed to Load Resources</h3>
          <p className="text-red-800 dark:text-red-200 mb-3 whitespace-pre-wrap">{resourcesError}</p>
          <div className="flex gap-3 flex-wrap">
            <button
              onClick={() => onFetchResources(true, environment)}
              className="px-4 py-2 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 transition-colors"
            >
              Retry
            </button>
            {(resourcesError.includes('Azure CLI is not installed') ||
              resourcesError.includes('Azure CLI not found')) && (
              <button
                onClick={async () => {
                  try {
                    const response = await invoke<CommandResponse>('install_azure_cli');
                    if (response.success) {
                      alert('Azure CLI installation started. Please restart the application after installation completes.');
                    } else {
                      alert(`Failed to install Azure CLI: ${response.error || 'Unknown error'}`);
                    }
                  } catch (error) {
                    alert(`Error installing Azure CLI: ${error}`);
                  }
                }}
                className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors"
              >
                📦 Install Azure CLI
              </button>
            )}
            {(resourcesError.includes('Could not find Mystira.DevHub.CLI') ||
              resourcesError.includes('Program not found') ||
              resourcesError.includes('Failed to spawn process')) && (
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

      {!resourcesLoading && !resourcesError && (
        <ResourceGrid
          resources={resources}
          onRefresh={() => onFetchResources(true, environment)}
          onDelete={async (resourceId: string) => {
            try {
              const response = await invoke<CommandResponse>('delete_azure_resource', {
                resourceId,
              });
              if (response.success) {
                onFetchResources(true, environment);
              } else {
                throw new Error(response.error || 'Failed to delete resource');
              }
            } catch (error) {
              throw error;
            }
          }}
        />
      )}

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

