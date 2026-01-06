import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useConnectionStore } from '../connectionStore';
import { mockTauriInvoke, mockConnectionTestSuccess, mockConnectionTestError } from '../../test/utils';

describe('connectionStore', () => {
  beforeEach(() => {
    // Reset store state before each test
    useConnectionStore.getState().reset();
    vi.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have correct initial connections', () => {
      const { connections } = useConnectionStore.getState();

      expect(connections).toHaveLength(4);
      expect(connections[0].name).toBe('Cosmos DB');
      expect(connections[1].name).toBe('Azure CLI');
      expect(connections[2].name).toBe('GitHub CLI');
      expect(connections[3].name).toBe('Blob Storage');
    });

    it('should start with checking status', () => {
      const { connections, isChecking, lastChecked } = useConnectionStore.getState();

      expect(connections.every(c => c.status === 'checking')).toBe(true);
      expect(isChecking).toBe(false);
      expect(lastChecked).toBeNull();
    });
  });

  describe('testConnection', () => {
    it('should update connection status on success', async () => {
      await mockTauriInvoke('test_connection', mockConnectionTestSuccess('azurecli', {
        user: 'test@example.com',
      }));

      await useConnectionStore.getState().testConnection('azurecli');

      const { connections } = useConnectionStore.getState();
      const azureConnection = connections.find(c => c.type === 'azurecli');

      expect(azureConnection?.status).toBe('connected');
      expect(azureConnection?.details).toBe('test@example.com');
      expect(azureConnection?.error).toBeUndefined();
    });

    it('should update connection status on failure', async () => {
      await mockTauriInvoke('test_connection', mockConnectionTestError('Connection refused'));

      await useConnectionStore.getState().testConnection('azurecli');

      const { connections } = useConnectionStore.getState();
      const azureConnection = connections.find(c => c.type === 'azurecli');

      expect(azureConnection?.status).toBe('disconnected');
      expect(azureConnection?.error).toBe('Connection refused');
    });

    it('should handle exceptions gracefully', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      invoke.mockRejectedValueOnce(new Error('Network error'));

      await useConnectionStore.getState().testConnection('azurecli');

      const { connections } = useConnectionStore.getState();
      const azureConnection = connections.find(c => c.type === 'azurecli');

      expect(azureConnection?.status).toBe('disconnected');
      expect(azureConnection?.error).toBe('Network error');
    });
  });

  describe('testConnections', () => {
    it('should test all connections sequentially', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      invoke.mockResolvedValue(mockConnectionTestSuccess('test', {}));

      await useConnectionStore.getState().testConnections();

      const { isChecking, lastChecked } = useConnectionStore.getState();

      expect(isChecking).toBe(false);
      expect(lastChecked).toBeInstanceOf(Date);
      expect(invoke).toHaveBeenCalledTimes(4); // 4 connections
    });

    it('should set isChecking during operation', async () => {
      const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
      let checkingDuringCall = false;

      invoke.mockImplementation(async () => {
        checkingDuringCall = useConnectionStore.getState().isChecking;
        return mockConnectionTestSuccess('test', {});
      });

      await useConnectionStore.getState().testConnections();

      expect(checkingDuringCall).toBe(true);
      expect(useConnectionStore.getState().isChecking).toBe(false);
    });
  });

  describe('setConnectionStatus', () => {
    it('should update specific connection status', () => {
      useConnectionStore.getState().setConnectionStatus('azurecli', {
        status: 'connected',
        details: 'Updated details',
      });

      const { connections } = useConnectionStore.getState();
      const azureConnection = connections.find(c => c.type === 'azurecli');

      expect(azureConnection?.status).toBe('connected');
      expect(azureConnection?.details).toBe('Updated details');
    });

    it('should not affect other connections', () => {
      useConnectionStore.getState().setConnectionStatus('azurecli', {
        status: 'connected',
      });

      const { connections } = useConnectionStore.getState();
      const cosmosConnection = connections.find(c => c.type === 'cosmos');

      expect(cosmosConnection?.status).toBe('checking'); // Still initial state
    });
  });

  describe('reset', () => {
    it('should reset to initial state', async () => {
      // Modify state
      await useConnectionStore.getState().testConnections();
      useConnectionStore.getState().setConnectionStatus('azurecli', {
        status: 'connected',
        details: 'Test',
      });

      // Reset
      useConnectionStore.getState().reset();

      const { connections, isChecking, lastChecked } = useConnectionStore.getState();

      expect(connections.every(c => c.status === 'checking')).toBe(true);
      expect(isChecking).toBe(false);
      expect(lastChecked).toBeNull();
    });
  });
});
