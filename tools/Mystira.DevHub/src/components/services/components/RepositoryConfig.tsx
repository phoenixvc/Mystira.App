import { open as openDialog } from '@tauri-apps/api/dialog';
import { invoke } from '@tauri-apps/api/tauri';

interface RepositoryConfigProps {
  repoRoot: string;
  currentBranch: string;
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
  const pickRepoRoot = async () => {
    try {
      const selected = await openDialog({
        directory: true,
        multiple: false,
        defaultPath: repoRoot || undefined,
      });
      
      if (selected && typeof selected === 'string') {
        onRepoRootChange(selected);
        
        // Get current branch
        try {
          await invoke<string>('get_current_branch', { repoRoot: selected });
          // Branch will be set by parent component
        } catch (error) {
          console.warn('Failed to get current branch:', error);
        }
      }
    } catch (error) {
      console.error('Failed to pick repo root:', error);
    }
  };

  return (
    <div className="mb-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <h2 className="text-lg font-semibold mb-3 text-gray-900 dark:text-white">Repository Configuration</h2>
      <div className="space-y-3">
        <div className="flex items-center gap-3">
          <label className="font-medium text-gray-700 dark:text-gray-300">Repository Root:</label>
          <input
            type="text"
            value={repoRoot}
            onChange={(e) => onRepoRootChange(e.target.value)}
            className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500"
            placeholder="C:\Users\smitj\repos\Mystira.App"
          />
          <button
            onClick={pickRepoRoot}
            className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Browse...
          </button>
        </div>
        {currentBranch && (
          <div className="flex items-center gap-3">
            <label className="font-medium text-gray-700 dark:text-gray-300">Current Branch:</label>
            <span className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded">{currentBranch}</span>
            <label className="flex items-center gap-2 text-gray-700 dark:text-gray-300">
              <input
                type="checkbox"
                checked={useCurrentBranch}
                onChange={(e) => onUseCurrentBranchChange(e.target.checked)}
              />
              <span>Use current branch directory</span>
            </label>
          </div>
        )}
      </div>
    </div>
  );
}

