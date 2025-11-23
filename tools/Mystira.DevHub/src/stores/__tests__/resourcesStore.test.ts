import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useResourcesStore } from '../resourcesStore';
import { mockTauriInvoke, mockAzureResourcesResponse, createTestResource } from '../../test/utils';

describe('resourcesStore', () => {
  beforeEach(() => {
    useResourcesStore.getState().reset();
    vi.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have empty resources', () => {
      const { resources, isLoading, error } = useResourcesStore.getState();

      expect(resources).toEqual([]);
      expect(isLoading).toBe(false);
      expect(error).toBeNull();
    });
  });

  describe('fetchResources', () => {
    it('should fetch and map resources successfully', async () => {
      const testResource = createTestResource();
      await mockTauriInvoke('get_azure_resources', mockAzureResourcesResponse([testResource]));

      await useResourcesStore.getState().fetchResources();

      const { resources, isLoading, error } = useResourcesStore.getState();

      expect(isLoading).toBe(false);
      expect(error).toBeNull();
      expect(resources).toHaveLength(1);
      expect(resources[0].name).toBe('testaccount');
      expect(resources[0].type).toBe('Microsoft.Storage/storageAccounts');
      expect(resources[0].region).toBe('eastus');
    });

    it('should set error on failure', async () => {
      await mockTauriInvoke('get_azure_resources', {
        success: false,
        error: 'Authentication failed',
      });

      await useResourcesStore.getState().fetchResources();

      const { resources, isLoading, error } = useResourcesStore.getState();

      expect(isLoading).toBe(false);
      expect(error).toBe('Authentication failed');
      expect(resources).toEqual([]);
    });

    it('should respect cache', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      const testResource = createTestResource();
      invoke.mockResolvedValue(mockAzureResourcesResponse([testResource]));

      // First fetch
      await useResourcesStore.getState().fetchResources();
      expect(invoke).toHaveBeenCalledTimes(1);

      // Second fetch (should use cache)
      await useResourcesStore.getState().fetchResources();
      expect(invoke).toHaveBeenCalledTimes(1); // Still 1, cache was used
    });

    it('should bypass cache with forceRefresh', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      const testResource = createTestResource();
      invoke.mockResolvedValue(mockAzureResourcesResponse([testResource]));

      // First fetch
      await useResourcesStore.getState().fetchResources();
      expect(invoke).toHaveBeenCalledTimes(1);

      // Force refresh
      await useResourcesStore.getState().fetchResources(true);
      expect(invoke).toHaveBeenCalledTimes(2); // Cache bypassed
    });

    it('should prevent duplicate concurrent requests', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      invoke.mockImplementation(() => new Promise(resolve => setTimeout(() => resolve(mockAzureResourcesResponse([])), 100)));

      // Start two fetches concurrently
      const promise1 = useResourcesStore.getState().fetchResources(true);
      const promise2 = useResourcesStore.getState().fetchResources(true);

      await Promise.all([promise1, promise2]);

      // Should only call once (second call exits early)
      expect(invoke).toHaveBeenCalledTimes(1);
    });
  });

  describe('clearCache', () => {
    it('should clear cache timestamps', async () => {
      await mockTauriInvoke('get_azure_resources', mockAzureResourcesResponse([createTestResource()]));
      await useResourcesStore.getState().fetchResources();

      const { cacheValidUntil, lastFetched } = useResourcesStore.getState();
      expect(cacheValidUntil).not.toBeNull();
      expect(lastFetched).not.toBeNull();

      useResourcesStore.getState().clearCache();

      const state = useResourcesStore.getState();
      expect(state.cacheValidUntil).toBeNull();
      expect(state.lastFetched).toBeNull();
    });
  });

  describe('reset', () => {
    it('should reset to initial state', async () => {
      await mockTauriInvoke('get_azure_resources', mockAzureResourcesResponse([createTestResource()]));
      await useResourcesStore.getState().fetchResources();

      useResourcesStore.getState().reset();

      const { resources, isLoading, error, lastFetched, cacheValidUntil } = useResourcesStore.getState();

      expect(resources).toEqual([]);
      expect(isLoading).toBe(false);
      expect(error).toBeNull();
      expect(lastFetched).toBeNull();
      expect(cacheValidUntil).toBeNull();
    });
  });
});
