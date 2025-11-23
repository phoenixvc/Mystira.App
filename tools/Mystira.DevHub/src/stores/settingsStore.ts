import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface Settings {
  theme: 'light' | 'dark' | 'auto';
  defaultExportPath: string;
  defaultLogsPath: string;
  notificationsEnabled: boolean;
  notificationSound: boolean;
  logLevel: 'error' | 'warn' | 'info' | 'debug';
  autoUpdate: boolean;
  cacheEnabled: boolean;
  cacheDuration: number; // in minutes
}

interface SettingsState extends Settings {
  // Actions
  setTheme: (theme: Settings['theme']) => void;
  setDefaultExportPath: (path: string) => void;
  setDefaultLogsPath: (path: string) => void;
  setNotificationsEnabled: (enabled: boolean) => void;
  setNotificationSound: (enabled: boolean) => void;
  setLogLevel: (level: Settings['logLevel']) => void;
  setAutoUpdate: (enabled: boolean) => void;
  setCacheEnabled: (enabled: boolean) => void;
  setCacheDuration: (duration: number) => void;
  reset: () => void;
}

const defaultSettings: Settings = {
  theme: 'light',
  defaultExportPath: '',
  defaultLogsPath: '',
  notificationsEnabled: true,
  notificationSound: false,
  logLevel: 'info',
  autoUpdate: true,
  cacheEnabled: true,
  cacheDuration: 5,
};

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set) => ({
      ...defaultSettings,

      setTheme: (theme) => set({ theme }),
      setDefaultExportPath: (path) => set({ defaultExportPath: path }),
      setDefaultLogsPath: (path) => set({ defaultLogsPath: path }),
      setNotificationsEnabled: (enabled) => set({ notificationsEnabled: enabled }),
      setNotificationSound: (enabled) => set({ notificationSound: enabled }),
      setLogLevel: (level) => set({ logLevel: level }),
      setAutoUpdate: (enabled) => set({ autoUpdate: enabled }),
      setCacheEnabled: (enabled) => set({ cacheEnabled: enabled }),
      setCacheDuration: (duration) => set({ cacheDuration: duration }),
      reset: () => set(defaultSettings),
    }),
    {
      name: 'devhub-settings',
      storage: createJSONStorage(() => localStorage),
    }
  )
);
