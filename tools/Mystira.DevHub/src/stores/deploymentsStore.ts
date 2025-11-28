import { invoke } from '@tauri-apps/api/tauri';
import { create } from 'zustand';
import type { CommandResponse, Deployment, GitHubWorkflowRun } from '../types';

interface DeploymentsState {
  deployments: Deployment[];
  isLoading: boolean;
  error: string | null;
  lastFetched: Date | null;
  cacheValidUntil: Date | null;
  repository: string;

  // Actions
  fetchDeployments: (forceRefresh?: boolean) => Promise<void>;
  setRepository: (repository: string) => void;
  clearCache: () => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 3 * 60 * 1000; // 3 minutes (shorter for deployments)

export const useDeploymentsStore = create<DeploymentsState>((set, get) => ({
  deployments: [],
  isLoading: false,
  error: null,
  lastFetched: null,
  cacheValidUntil: null,
  repository: 'phoenixvc/Mystira.App',

  fetchDeployments: async (forceRefresh = false, limit?: number) => {
    const { cacheValidUntil, isLoading, repository } = get();

    // Check if cache is still valid
    if (!forceRefresh && cacheValidUntil && new Date() < cacheValidUntil) {
      console.log('Using cached deployments');
      return;
    }

    // Prevent duplicate requests
    if (isLoading) {
      console.log('Already fetching deployments');
      return;
    }

    set({ isLoading: true, error: null });

    try {
      // Try using the existing command first
      const response = await invoke<CommandResponse<GitHubWorkflowRun[]>>('get_github_deployments', {
        repository,
        limit: limit || 20,
      });

      if (response.success && response.result) {
        const mappedDeployments: Deployment[] = response.result.map((run, index) => {
          // Determine action type from workflow name
          let action: 'deploy' | 'validate' | 'preview' | 'destroy' = 'deploy';
          if (run.name?.toLowerCase().includes('validate')) action = 'validate';
          else if (run.name?.toLowerCase().includes('preview') || run.name?.toLowerCase().includes('what-if')) action = 'preview';
          else if (run.name?.toLowerCase().includes('destroy')) action = 'destroy';

          // Map GitHub conclusion to our status
          let status: 'success' | 'failed' | 'in_progress' = 'in_progress';
          if (run.conclusion === 'success') status = 'success';
          else if (run.conclusion === 'failure' || run.conclusion === 'cancelled') status = 'failed';
          else if (run.status === 'completed') status = 'success';

          // Calculate duration
          let duration = 'N/A';
          if (run.created_at && run.updated_at) {
            const start = new Date(run.created_at).getTime();
            const end = new Date(run.updated_at).getTime();
            const diffSeconds = Math.floor((end - start) / 1000);
            const minutes = Math.floor(diffSeconds / 60);
            const seconds = diffSeconds % 60;
            duration = `${minutes}m ${seconds}s`;
          }

          return {
            id: run.id?.toString() || index.toString(),
            timestamp: run.created_at || new Date().toISOString(),
            action,
            status,
            duration,
            resourcesAffected: 0,
            user: run.actor?.login || 'GitHub Actions',
            message: run.display_title || run.name || 'Workflow run',
            githubUrl: run.html_url,
          };
        });

        const now = new Date();
        set({
          deployments: mappedDeployments,
          isLoading: false,
          error: null,
          lastFetched: now,
          cacheValidUntil: new Date(now.getTime() + CACHE_DURATION_MS),
        });
      } else {
        set({
          isLoading: false,
          error: response.error || 'Failed to fetch GitHub deployments',
        });
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : String(error);
      // Check if it's an "Unknown command" error - the backend command might not be implemented yet
      if (errorMessage.includes('Unknown command') || errorMessage.includes('command')) {
        set({
          isLoading: false,
          error: 'Deployment history feature is not yet available. The backend command needs to be implemented.',
        });
      } else {
        set({
          isLoading: false,
          error: errorMessage,
        });
      }
    }
  },

  setRepository: (repository: string) => {
    set({ repository });
    get().clearCache();
  },

  clearCache: () => {
    set({
      cacheValidUntil: null,
      lastFetched: null,
    });
  },

  reset: () => {
    set({
      deployments: [],
      isLoading: false,
      error: null,
      lastFetched: null,
      cacheValidUntil: null,
      repository: 'phoenixvc/Mystira.App',
    });
  },
}));
