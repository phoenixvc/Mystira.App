import { EnvironmentPresetSelector, type EnvironmentPreset } from '../../services';
import { InfrastructureStatusIndicator } from './InfrastructureStatusIndicator';
import { RepositoryConfig } from './RepositoryConfig';

interface ServiceManagerHeaderProps {
  repoRoot: string;
  currentBranch: string | null;
  useCurrentBranch: boolean;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  infrastructureStatus: {
    dev: { exists: boolean; checking: boolean };
    prod: { exists: boolean; checking: boolean };
  };
  allRunning: boolean;
  anyRunning: boolean;
  anyBuilding: boolean;
  onRepoRootChange: (root: string) => void;
  onUseCurrentBranchChange: (use: boolean) => void;
  onApplyPreset: (preset: EnvironmentPreset) => void;
  onBuildAll: () => void;
  onStartAll: () => void;
  onStopAll: () => void;
}

export function ServiceManagerHeader({
  repoRoot,
  currentBranch,
  useCurrentBranch,
  serviceEnvironments,
  infrastructureStatus,
  allRunning,
  anyRunning,
  anyBuilding,
  onRepoRootChange,
  onUseCurrentBranchChange,
  onApplyPreset,
  onBuildAll,
  onStartAll,
  onStopAll,
}: ServiceManagerHeaderProps) {
  return (
    <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div className="flex items-center gap-4 flex-1 min-w-0">
          <h1 className="text-xl font-bold text-gray-900 dark:text-white font-mono">SERVICE MANAGER</h1>
          <span className="text-xs text-gray-500 dark:text-gray-400 hidden sm:inline">
            Ctrl+Shift+S (Start All) | Ctrl+Shift+X (Stop All) | Ctrl+R (Refresh)
          </span>
          <InfrastructureStatusIndicator
            serviceEnvironments={serviceEnvironments}
            infrastructureStatus={infrastructureStatus}
          />
        </div>
        <div className="flex gap-2 items-center flex-shrink-0">
          <EnvironmentPresetSelector
            currentEnvironments={serviceEnvironments}
            onApplyPreset={onApplyPreset}
            onSaveCurrent={() => {}}
          />
          <button
            onClick={onBuildAll}
            disabled={anyBuilding}
            className="px-3 py-1.5 bg-blue-600 text-white rounded text-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            title="Build all local services"
          >
            {anyBuilding ? 'Building...' : 'Build All'}
          </button>
          {!allRunning && (
            <button
              onClick={onStartAll}
              disabled={anyRunning || anyBuilding}
              className="px-3 py-1.5 bg-green-600 text-white rounded text-sm hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Start All
            </button>
          )}
          {anyRunning && (
            <button
              onClick={onStopAll}
              className="px-3 py-1.5 bg-red-600 text-white rounded text-sm hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
            >
              Stop All
            </button>
          )}
        </div>
      </div>
      <RepositoryConfig
        repoRoot={repoRoot}
        currentBranch={currentBranch}
        useCurrentBranch={useCurrentBranch}
        onRepoRootChange={onRepoRootChange}
        onUseCurrentBranchChange={onUseCurrentBranchChange}
      />
    </div>
  );
}

