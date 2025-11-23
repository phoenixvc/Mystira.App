import { create } from 'zustand';
import { invoke } from '@tauri-apps/api/tauri';

export interface AzureResource {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'unknown';
  region: string;
  costToday: number;
  lastUpdated: string;
  properties: Record<string, string>;
}

interface ResourcesState {
  resources: AzureResource[];
  isLoading: boolean;
  error: string | null;
  lastFetched: Date | null;
  cacheValidUntil: Date | null;

  // Actions
  fetchResources: (forceRefresh?: boolean) => Promise<void>;
  clearCache: () => void;
  reset: () => void;
}

const CACHE_DURATION_MS = 5 * 60 * 1000; // 5 minutes

export const useResourcesStore = create<ResourcesState>((set, get) => ({
  resources: [],
  isLoading: false,
  error: null,
  lastFetched: null,
  cacheValidUntil: null,

  fetchResources: async (forceRefresh = false) => {
    const { cacheValidUntil, isLoading } = get();

    // Check if cache is still valid
    if (!forceRefresh && cacheValidUntil && new Date() < cacheValidUntil) {
      console.log('Using cached resources');
      return;
    }

    // Prevent duplicate requests
    if (isLoading) {
      console.log('Already fetching resources');
      return;
    }

    set({ isLoading: true, error: null });

    try {
      const response: any = await invoke('get_azure_resources', {
        subscriptionId: null,
        resourceGroup: null,
      });

      if (response.success && response.result) {
        const mappedResources: AzureResource[] = response.result.map((resource: any) => ({
          id: resource.id,
          name: resource.name,
          type: resource.type,
          status: 'running' as const,
          region: resource.location || 'Unknown',
          costToday: 0,
          lastUpdated: new Date().toISOString(),
          properties: {
            'Resource Group': resource.resourceGroup || 'N/A',
            'SKU': resource.sku?.name || 'N/A',
            'Kind': resource.kind || 'N/A',
          },
        }));

        const now = new Date();
        set({
          resources: mappedResources,
          isLoading: false,
          error: null,
          lastFetched: now,
          cacheValidUntil: new Date(now.getTime() + CACHE_DURATION_MS),
        });
      } else {
        set({
          isLoading: false,
          error: response.error || 'Failed to fetch Azure resources',
        });
      }
    } catch (error) {
      set({
        isLoading: false,
        error: String(error),
      });
    }
  },

  clearCache: () => {
    set({
      cacheValidUntil: null,
      lastFetched: null,
    });
  },

  reset: () => {
    set({
      resources: [],
      isLoading: false,
      error: null,
      lastFetched: null,
      cacheValidUntil: null,
    });
  },
}));
