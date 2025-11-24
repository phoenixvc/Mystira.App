import { ServiceConfig } from './types';

interface ViewModeSelectorProps {
  config: ServiceConfig;
  currentMode: 'logs' | 'webview' | 'split';
  isMaximized: boolean;
  onModeChange: (mode: 'logs' | 'webview' | 'split') => void;
  onMaximize: () => void;
  onOpenInBrowser: (url: string) => void;
  onOpenInTauriWindow: (url: string, title: string) => void;
  onClearLogs: () => void;
  hasLogs: boolean;
}

export function ViewModeSelector({
  config,
  currentMode,
  isMaximized,
  onModeChange,
  onMaximize,
  onOpenInBrowser,
  onOpenInTauriWindow,
  onClearLogs,
  hasLogs,
}: ViewModeSelectorProps) {
  return (
    <div className="mt-2 flex gap-2 flex-wrap items-center">
      <button
        onClick={() => onOpenInBrowser(config.url!)}
        className="px-3 py-1 bg-blue-500 text-white rounded text-sm hover:bg-blue-600"
      >
        Open in External Browser
      </button>
      <div className="flex gap-1 border border-gray-300 dark:border-gray-600 rounded overflow-hidden">
        <button
          onClick={() => onModeChange('logs')}
          className={`px-3 py-1 text-sm ${
            currentMode === 'logs' || !currentMode
              ? 'bg-gray-600 text-white'
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
          title="Show logs only"
        >
          Logs
        </button>
        <button
          onClick={() => onModeChange('split')}
          className={`px-3 py-1 text-sm border-l border-r border-gray-300 dark:border-gray-600 ${
            currentMode === 'split'
              ? 'bg-gray-600 text-white'
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
          title="Show logs and webview side by side"
        >
          Split
        </button>
        <button
          onClick={() => onModeChange('webview')}
          className={`px-3 py-1 text-sm ${
            currentMode === 'webview'
              ? 'bg-gray-600 text-white'
              : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
          }`}
          title="Show webview only"
        >
          Webview
        </button>
      </div>
      {config.url && (
        <button
          onClick={() => {
            const isHttps = config.url?.startsWith('https://');
            if (isHttps) {
              onOpenInTauriWindow(config.url, config.displayName);
            } else {
              onOpenInBrowser(config.url);
            }
          }}
          className="px-3 py-1 bg-green-500 text-white rounded text-sm hover:bg-green-600"
          title={config.url?.startsWith('https://') 
            ? "Open in Tauri window (handles self-signed certificates)" 
            : "Open in external browser"}
        >
          {config.url?.startsWith('https://') ? '🪟 Open Window' : '🌐 Open in Browser'}
        </button>
      )}
      <button
        onClick={onMaximize}
        className="px-3 py-1 bg-indigo-500 text-white rounded text-sm hover:bg-indigo-600"
        title={isMaximized ? "Restore view" : "Maximize view"}
      >
        {isMaximized ? '↗ Restore' : '⛶ Maximize'}
      </button>
      {hasLogs && (
        <button
          onClick={onClearLogs}
          className="px-3 py-1 bg-red-500 text-white rounded text-sm hover:bg-red-600"
        >
          Clear Logs
        </button>
      )}
    </div>
  );
}

