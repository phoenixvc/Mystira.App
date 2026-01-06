import { invoke } from '@tauri-apps/api/tauri';
import { EnvironmentPresetSelector, type EnvironmentPreset } from '../environment';

interface InfrastructureStatus {
  dev: { exists: boolean; checking: boolean };
  prod: { exists: boolean; checking: boolean };
}

interface ServiceManagerHeaderProps {
  repoRoot: string;
  currentBranch: string | null;
  useCurrentBranch: boolean;
  onRepoRootChange: (root: string) => void;
  onUseCurrentBranchChange: (use: boolean) => void;
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  onServiceEnvironmentsChange: (environments: Record<string, 'local' | 'dev' | 'prod'>) => void;
  infrastructureStatus: InfrastructureStatus;
  onApplyPreset: (preset: EnvironmentPreset) => void;
  onBuildAll: () => void;
  onStartAll: () => void;
  onStopAll: () => void;
  anyBuilding: boolean;
  allRunning: boolean;
  anyRunning: boolean;
  onCheckEnvironmentHealth: (serviceName: string, environment: 'dev' | 'prod') => void;
}

export function ServiceManagerHeader({
  repoRoot,
  currentBranch,
  useCurrentBranch,
  onRepoRootChange,
  onUseCurrentBranchChange,
  serviceEnvironments,
  onServiceEnvironmentsChange,
  infrastructureStatus,
  onApplyPreset,
  onBuildAll,
  onStartAll,
  onStopAll,
  anyBuilding,
  allRunning,
  anyRunning,
  onCheckEnvironmentHealth,
}: ServiceManagerHeaderProps) {
  const handleApplyPreset = (preset: EnvironmentPreset) => {
    const hasProd = Object.values(preset.environments).includes('prod');
    if (hasProd) {
      const confirmed = window.confirm('⚠️ WARNING: This preset includes PRODUCTION environments.\n\nAre you sure you want to apply this preset?');
      if (!confirmed) return;
    }

    onServiceEnvironmentsChange(preset.environments);
    localStorage.setItem('serviceEnvironments', JSON.stringify(preset.environments));

    Object.entries(preset.environments).forEach(([serviceName, env]) => {
      if (env !== 'local' && (env === 'dev' || env === 'prod')) {
        onCheckEnvironmentHealth(serviceName, env as 'dev' | 'prod');
      }
    });

    onApplyPreset(preset);
  };

  const handleBrowseRepo = async () => {
    try {
      const { open } = await import('@tauri-apps/api/dialog');
      const selected = await open({
        directory: true,
        multiple: false,
        defaultPath: repoRoot || undefined,
      });
      
      if (selected && typeof selected === 'string') {
        onRepoRootChange(selected);
        try {
          await invoke<string>('get_current_branch', { repoRoot: selected });
        } catch (error) {
          console.warn('Failed to get current branch:', error);
        }
      }
    } catch (error) {
      console.error('Failed to pick repo root:', error);
    }
  };

  const hasDevServices = Object.values(serviceEnvironments).includes('dev');
  const hasProdServices = Object.values(serviceEnvironments).includes('prod');
  const devStatus = infrastructureStatus.dev;
  const prodStatus = infrastructureStatus.prod;

  return (
    <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <div className="flex items-center gap-4 flex-1 min-w-0">
          <h1 className="text-xl font-bold text-gray-900 dark:text-white font-mono">SERVICE MANAGER</h1>
          <span className="text-xs text-gray-500 dark:text-gray-400 hidden sm:inline">
            Ctrl+Shift+B (Build) | Ctrl+Shift+S (Start) | Ctrl+Shift+X (Stop) | Ctrl+R (Refresh)
          </span>
          
          {(hasDevServices || hasProdServices) && (
            <div className="flex items-center gap-2 text-xs">
              {hasDevServices && (
                <div className="flex items-center gap-1 px-2 py-1 rounded" 
                     style={{ 
                       backgroundColor: devStatus.checking 
                         ? '#fef3c7' 
                         : devStatus.exists 
                           ? '#d1fae5' 
                           : '#fee2e2',
                       color: devStatus.checking 
                         ? '#92400e' 
                         : devStatus.exists 
                           ? '#065f46' 
                           : '#991b1b'
                     }}>
                  {devStatus.checking ? '⏳' : devStatus.exists ? '✅' : '⚠️'}
                  <span className="font-medium">DEV</span>
                  {!devStatus.exists && !devStatus.checking && (
                    <button
                      onClick={() => {
                        window.dispatchEvent(new CustomEvent('navigate-to-infrastructure'));
                      }}
                      className="ml-1 underline hover:no-underline"
                      title="Deploy missing infrastructure"
                    >
                      Deploy
                    </button>
                  )}
                </div>
              )}
              {hasProdServices && (
                <div className="flex items-center gap-1 px-2 py-1 rounded"
                     style={{ 
                       backgroundColor: prodStatus.checking 
                         ? '#fef3c7' 
                         : prodStatus.exists 
                           ? '#d1fae5' 
                           : '#fee2e2',
                       color: prodStatus.checking 
                         ? '#92400e' 
                         : prodStatus.exists 
                           ? '#065f46' 
                           : '#991b1b'
                     }}>
                  {prodStatus.checking ? '⏳' : prodStatus.exists ? '✅' : '⚠️'}
                  <span className="font-medium">PROD</span>
                  {!prodStatus.exists && !prodStatus.checking && (
                    <button
                      onClick={() => {
                        window.dispatchEvent(new CustomEvent('navigate-to-infrastructure'));
                      }}
                      className="ml-1 underline hover:no-underline"
                      title="Deploy missing infrastructure"
                    >
                      Deploy
                    </button>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
        <div className="flex gap-2 items-center flex-shrink-0">
          <EnvironmentPresetSelector
            currentEnvironments={serviceEnvironments}
            onApplyPreset={handleApplyPreset}
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
      <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 flex items-center gap-3 flex-wrap">
        <div className="flex items-center gap-2 flex-1 min-w-0">
          <label className="text-xs font-medium text-gray-600 dark:text-gray-400 whitespace-nowrap">Repo:</label>
          <input
            type="text"
            value={repoRoot}
            onChange={(e) => onRepoRootChange(e.target.value)}
            className="flex-1 min-w-0 px-2 py-1 text-xs border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
            placeholder="C:\Users\smitj\repos\Mystira.App"
          />
          <button
            onClick={handleBrowseRepo}
            className="px-2 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 flex-shrink-0"
          >
            Browse
          </button>
        </div>
        {currentBranch && (
          <div className="flex items-center gap-2 flex-shrink-0">
            <span className="text-xs text-gray-600 dark:text-gray-400">Branch:</span>
            <span className="px-2 py-0.5 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded font-mono">{currentBranch}</span>
            <label className="flex items-center gap-1 text-xs text-gray-600 dark:text-gray-400">
              <input
                type="checkbox"
                checked={useCurrentBranch}
                onChange={(e) => onUseCurrentBranchChange(e.target.checked)}
                className="w-3 h-3"
              />
              <span>Use branch dir</span>
            </label>
          </div>
        )}
      </div>
    </div>
  );
}

