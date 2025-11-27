import { invoke } from '@tauri-apps/api/tauri';
import { create } from 'zustand';
import type { AzureResource, AzureResourceMapped, CommandResponse } from '../types';

interface ResourcesState {
  resources: AzureResourceMapped[];
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
      const response = await invoke<CommandResponse<AzureResource[]>>('get_azure_resources', {
        subscriptionId: null,
      });

      if (response.success && response.result) {
        const mappedResources: AzureResourceMapped[] = response.result.map((resource) => ({
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
        // Check if Azure CLI is missing and prompt for installation
        if (response.result && typeof response.result === 'object') {
          const result = response.result as any;
          if (result.azureCliMissing && result.wingetAvailable) {
            const shouldInstall = confirm(
              'Azure CLI is not installed. Would you like to install it now using winget?\n\n' +
              'This will open a terminal window to install Azure CLI. After installation, please restart the application.'
            );
            
            if (shouldInstall) {
              try {
                const installResponse = await invoke<CommandResponse>('install_azure_cli');
                if (installResponse.success) {
                  const result = installResponse.result as any;
                  if (result?.requiresRestart) {
                    alert('A terminal window has opened to install Azure CLI. After installation completes in that window, please RESTART the application for Azure CLI to be detected.\n\nNote: If Azure CLI was already installed, you may need to restart the app for it to be detected in the PATH.');
                  } else {
                    alert('A terminal window has opened to install Azure CLI. Please wait for installation to complete in that window, then restart the application.');
                  }
                } else {
                  alert(`Failed to install Azure CLI: ${installResponse.error || 'Unknown error'}\n\nPlease install manually from https://aka.ms/installazurecliwindows`);
                }
              } catch (error) {
                alert(`Error installing Azure CLI: ${error}\n\nPlease install manually from https://aka.ms/installazurecliwindows`);
              }
            }
          }
        }
        
        set({
          isLoading: false,
          error: response.error || 'Failed to fetch Azure resources',
        });
      }
    } catch (error) {
      set({
        isLoading: false,
        error: error instanceof Error ? error.message : String(error),
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
