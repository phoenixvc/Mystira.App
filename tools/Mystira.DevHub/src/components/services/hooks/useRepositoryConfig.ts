import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';

export function useRepositoryConfig() {
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [currentBranch, setCurrentBranch] = useState<string>('');
  const [useCurrentBranch, setUseCurrentBranch] = useState<boolean>(false);

  useEffect(() => {
    const initialize = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
        try {
          const branch = await invoke<string>('get_current_branch', { repoRoot: root });
          setCurrentBranch(branch);
        } catch (error) {
          console.error('Failed to get current branch:', error);
        }
      } catch (error) {
        console.error('Failed to get repo root:', error);
        setRepoRoot('C:\\Users\\smitj\\repos\\Mystira.App');
      }
    };
    
    initialize();
  }, []);

  const updateRepoRoot = async (newRoot: string) => {
    setRepoRoot(newRoot);
    try {
      const branch = await invoke<string>('get_current_branch', { repoRoot: newRoot });
      setCurrentBranch(branch);
    } catch (error) {
      console.error('Failed to get current branch:', error);
      setCurrentBranch('');
    }
  };

  return {
    repoRoot,
    currentBranch,
    useCurrentBranch,
    setRepoRoot: updateRepoRoot,
    setUseCurrentBranch,
  };
}

