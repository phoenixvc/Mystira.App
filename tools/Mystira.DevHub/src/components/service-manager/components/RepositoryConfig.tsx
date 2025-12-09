import { invoke } from '@tauri-apps/api/tauri';

interface RepositoryConfigProps {
  repoRoot: string;
  currentBranch: string | null;
  useCurrentBranch: boolean;
  onRepoRootChange: (root: string) => void;
  onUseCurrentBranchChange: (use: boolean) => void;
}

export function RepositoryConfig({
  repoRoot,
  currentBranch,
  useCurrentBranch,
  onRepoRootChange,
  onUseCurrentBranchChange,
}: RepositoryConfigProps) {
  const handleBrowse = async () => {
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

  return (
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
          onClick={handleBrowse}
          className="px-2 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 flex-shrink-0"
        >
          Browse
        </button>
      </div>
      {currentBranch && (
        <div className="flex items-center gap-2 flex-shrink-0">
          <span className="text-xs text-gray-600 dark:text-gray-400">Branch:</span>
          <span className="px-2 py-0.5 text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded font-mono">
            {currentBranch}
          </span>
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
  );
}

