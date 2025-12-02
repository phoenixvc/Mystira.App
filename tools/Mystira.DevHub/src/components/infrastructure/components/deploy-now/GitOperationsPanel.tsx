import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import {
  GitBranch,
  GitCommit,
  Upload,
  RefreshCw,
  CheckCircle2,
  AlertCircle,
  Loader2,
  FileEdit,
  ChevronDown,
  ChevronRight,
} from 'lucide-react';
import type { CommandResponse } from '../../../../types';

interface GitStatus {
  branch: string;
  hasUncommittedChanges: boolean;
  isRepository: boolean;
  uncommittedFiles: string[];
  aheadCount: number;
  behindCount: number;
}

interface GitOperationsPanelProps {
  repoRoot: string;
  onDeployTriggered?: () => void;
  disabled?: boolean;
}

export function GitOperationsPanel({
  repoRoot,
  onDeployTriggered,
  disabled = false,
}: GitOperationsPanelProps) {
  const [gitStatus, setGitStatus] = useState<GitStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isPushing, setIsPushing] = useState(false);
  const [isCommitting, setIsCommitting] = useState(false);
  const [commitMessage, setCommitMessage] = useState('Trigger deployment');
  const [error, setError] = useState<string | null>(null);
  const [showUncommitted, setShowUncommitted] = useState(false);
  const [pushResult, setPushResult] = useState<{ success: boolean; message: string } | null>(null);

  const fetchGitStatus = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await invoke<CommandResponse>('get_git_status', { repoRoot });

      if (response.success && response.result) {
        const result = response.result as GitStatus;
        setGitStatus(result);
      } else {
        setError(response.error || 'Failed to get git status');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to get git status');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchGitStatus();
  }, [repoRoot]);

  const handleCommit = async () => {
    if (!commitMessage.trim()) return;

    setIsCommitting(true);
    setError(null);

    try {
      // First stage all changes
      await invoke<CommandResponse>('git_stage_all', { repoRoot });

      // Then commit
      const response = await invoke<CommandResponse>('git_commit', {
        repoRoot,
        message: commitMessage,
      });

      if (response.success) {
        setCommitMessage('Trigger deployment');
        await fetchGitStatus();
      } else {
        setError(response.error || 'Failed to commit changes');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to commit changes');
    } finally {
      setIsCommitting(false);
    }
  };

  const handlePush = async (allowEmpty = false) => {
    if (!gitStatus) return;

    setIsPushing(true);
    setError(null);
    setPushResult(null);

    try {
      // If no commits ahead and allowEmpty, create an empty commit first
      if (gitStatus.aheadCount === 0 && allowEmpty) {
        const emptyCommitResponse = await invoke<CommandResponse>('git_commit_empty', {
          repoRoot,
          message: commitMessage || 'Trigger deployment',
        });

        if (!emptyCommitResponse.success) {
          setError(emptyCommitResponse.error || 'Failed to create empty commit');
          setIsPushing(false);
          return;
        }
      }

      // Push to remote
      const response = await invoke<CommandResponse>('git_push', {
        repoRoot,
        branch: gitStatus.branch,
      });

      if (response.success) {
        setPushResult({
          success: true,
          message: `Successfully pushed to ${gitStatus.branch}. CI/CD pipeline triggered!`,
        });
        onDeployTriggered?.();
        await fetchGitStatus();
      } else {
        setError(response.error || 'Failed to push');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to push');
    } finally {
      setIsPushing(false);
    }
  };

  const handleSync = async () => {
    if (!gitStatus) return;

    setIsLoading(true);
    setError(null);

    try {
      const response = await invoke<CommandResponse>('git_sync', {
        repoRoot,
        branch: gitStatus.branch,
      });

      if (response.success) {
        await fetchGitStatus();
      } else {
        setError(response.error || 'Failed to sync');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to sync');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading && !gitStatus) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
        <div className="flex items-center justify-center py-8">
          <Loader2 className="w-6 h-6 text-blue-500 animate-spin mr-2" />
          <span className="text-gray-600 dark:text-gray-300">Loading git status...</span>
        </div>
      </div>
    );
  }

  if (!gitStatus?.isRepository) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4">
        <div className="flex items-center gap-2 text-yellow-600 dark:text-yellow-400">
          <AlertCircle className="w-5 h-5" />
          <span>Not in a git repository</span>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-700/50">
        <div className="flex items-center gap-3">
          <GitBranch className="w-4 h-4 text-orange-500" />
          <span className="font-medium text-gray-900 dark:text-white text-sm">
            Git Operations
          </span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={fetchGitStatus}
            disabled={isLoading}
            className="p-1 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 disabled:opacity-50"
            title="Refresh status"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="p-4 space-y-4">
        {/* Branch Info */}
        <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <div className="flex items-center gap-3">
            <GitBranch className="w-4 h-4 text-green-500" />
            <span className="font-mono text-sm text-gray-900 dark:text-white">
              {gitStatus.branch}
            </span>
          </div>
          <div className="flex items-center gap-2 text-xs">
            {gitStatus.aheadCount > 0 && (
              <span className="px-2 py-0.5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 rounded">
                {gitStatus.aheadCount} ahead
              </span>
            )}
            {gitStatus.behindCount > 0 && (
              <span className="px-2 py-0.5 bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 rounded">
                {gitStatus.behindCount} behind
              </span>
            )}
            {gitStatus.aheadCount === 0 && gitStatus.behindCount === 0 && (
              <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-600 text-gray-600 dark:text-gray-300 rounded">
                Up to date
              </span>
            )}
          </div>
        </div>

        {/* Uncommitted Changes */}
        {gitStatus.hasUncommittedChanges && (
          <div className="border border-yellow-200 dark:border-yellow-800 rounded-lg overflow-hidden">
            <button
              onClick={() => setShowUncommitted(!showUncommitted)}
              className="w-full flex items-center justify-between p-3 bg-yellow-50 dark:bg-yellow-900/20 text-left"
            >
              <div className="flex items-center gap-2">
                <AlertCircle className="w-4 h-4 text-yellow-500" />
                <span className="text-sm text-yellow-700 dark:text-yellow-300">
                  Uncommitted changes ({gitStatus.uncommittedFiles.length} files)
                </span>
              </div>
              {showUncommitted ? (
                <ChevronDown className="w-4 h-4 text-yellow-500" />
              ) : (
                <ChevronRight className="w-4 h-4 text-yellow-500" />
              )}
            </button>
            {showUncommitted && gitStatus.uncommittedFiles.length > 0 && (
              <div className="p-3 max-h-32 overflow-auto bg-gray-50 dark:bg-gray-700">
                {gitStatus.uncommittedFiles.slice(0, 10).map((file, idx) => (
                  <div key={idx} className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-300 py-0.5">
                    <FileEdit className="w-3 h-3" />
                    <span className="font-mono truncate">{file}</span>
                  </div>
                ))}
                {gitStatus.uncommittedFiles.length > 10 && (
                  <div className="text-xs text-gray-500 mt-1">
                    ...and {gitStatus.uncommittedFiles.length - 10} more
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* Commit Section */}
        {gitStatus.hasUncommittedChanges && (
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <GitCommit className="w-4 h-4 text-gray-500" />
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Commit Changes
              </span>
            </div>
            <div className="flex gap-2">
              <input
                type="text"
                value={commitMessage}
                onChange={(e) => setCommitMessage(e.target.value)}
                placeholder="Commit message..."
                disabled={isCommitting || disabled}
                className="flex-1 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white disabled:opacity-50"
              />
              <button
                onClick={handleCommit}
                disabled={isCommitting || !commitMessage.trim() || disabled}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {isCommitting ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <GitCommit className="w-4 h-4" />
                )}
                Commit
              </button>
            </div>
          </div>
        )}

        {/* Push Section */}
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <Upload className="w-4 h-4 text-gray-500" />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Deploy via Push
            </span>
          </div>
          <div className="flex gap-2">
            <button
              onClick={handleSync}
              disabled={isLoading || isPushing || disabled}
              className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
            >
              <RefreshCw className="w-4 h-4" />
              Sync
            </button>
            {gitStatus.aheadCount > 0 ? (
              <button
                onClick={() => handlePush(false)}
                disabled={isPushing || disabled}
                className="flex-1 px-4 py-2 text-sm font-medium text-white bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isPushing ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Upload className="w-4 h-4" />
                )}
                Push ({gitStatus.aheadCount} commits)
              </button>
            ) : (
              <button
                onClick={() => handlePush(true)}
                disabled={isPushing || disabled}
                className="flex-1 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                {isPushing ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Upload className="w-4 h-4" />
                )}
                Create Empty Commit & Push
              </button>
            )}
          </div>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Push will trigger CI/CD pipeline to deploy your code
          </p>
        </div>

        {/* Error Message */}
        {error && (
          <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
            <div className="flex items-center gap-2 text-red-700 dark:text-red-300 text-sm">
              <AlertCircle className="w-4 h-4" />
              <span>{error}</span>
            </div>
          </div>
        )}

        {/* Success Message */}
        {pushResult?.success && (
          <div className="p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
            <div className="flex items-center gap-2 text-green-700 dark:text-green-300 text-sm">
              <CheckCircle2 className="w-4 h-4" />
              <span>{pushResult.message}</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default GitOperationsPanel;
