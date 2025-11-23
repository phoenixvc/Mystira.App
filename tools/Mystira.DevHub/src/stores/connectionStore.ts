import { create } from 'zustand';
import { invoke } from '@tauri-apps/api/tauri';

export interface ConnectionStatus {
  name: string;
  type: 'cosmos' | 'storage' | 'azurecli' | 'githubcli';
  status: 'connected' | 'disconnected' | 'checking';
  icon: string;
  details?: string;
  error?: string;
}

interface ConnectionState {
  connections: ConnectionStatus[];
  isChecking: boolean;
  lastChecked: Date | null;

  // Actions
  testConnections: () => Promise<void>;
  testConnection: (type: string, connectionString?: string) => Promise<void>;
  setConnectionStatus: (type: string, status: Partial<ConnectionStatus>) => void;
  reset: () => void;
}

const initialConnections: ConnectionStatus[] = [
  {
    name: 'Cosmos DB',
    type: 'cosmos',
    status: 'checking',
    icon: '🗄️',
  },
  {
    name: 'Azure CLI',
    type: 'azurecli',
    status: 'checking',
    icon: '☁️',
  },
  {
    name: 'GitHub CLI',
    type: 'githubcli',
    status: 'checking',
    icon: '🐙',
  },
  {
    name: 'Blob Storage',
    type: 'storage',
    status: 'checking',
    icon: '📦',
  },
];

export const useConnectionStore = create<ConnectionState>((set, get) => ({
  connections: initialConnections,
  isChecking: false,
  lastChecked: null,

  testConnections: async () => {
    set({ isChecking: true });

    const { connections } = get();

    for (const conn of connections) {
      await get().testConnection(
        conn.type,
        conn.type === 'cosmos' || conn.type === 'storage'
          ? (process.env as any)[`${conn.type.toUpperCase()}_CONNECTION_STRING`]
          : undefined
      );
    }

    set({ isChecking: false, lastChecked: new Date() });
  },

  testConnection: async (type: string, connectionString?: string) => {
    try {
      const response: any = await invoke('test_connection', {
        connectionType: type,
        connectionString: connectionString || null,
      });

      if (response.success && response.result) {
        get().setConnectionStatus(type, {
          status: 'connected',
          details: getConnectionDetails(type, response.result),
          error: undefined,
        });
      } else {
        get().setConnectionStatus(type, {
          status: 'disconnected',
          error: response.error || 'Connection failed',
          details: undefined,
        });
      }
    } catch (error) {
      get().setConnectionStatus(type, {
        status: 'disconnected',
        error: String(error),
        details: undefined,
      });
    }
  },

  setConnectionStatus: (type: string, status: Partial<ConnectionStatus>) => {
    set((state) => ({
      connections: state.connections.map((c) =>
        c.type === type ? { ...c, ...status } : c
      ),
    }));
  },

  reset: () => {
    set({
      connections: initialConnections,
      isChecking: false,
      lastChecked: null,
    });
  },
}));

function getConnectionDetails(type: string, result: any): string {
  switch (type) {
    case 'cosmos':
      return result.accountName || 'Connected';
    case 'storage':
      return result.accountName || 'Connected';
    case 'azurecli':
      return result.user || 'Authenticated';
    case 'githubcli':
      return result.status === 'authenticated' ? 'Authenticated' : 'Connected';
    default:
      return 'Connected';
  }
}
