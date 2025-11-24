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
  const isHttps = config.url?.startsWith('https://');
  
  return (
    <div className="mt-2 flex items-center gap-1.5 flex-wrap">
      {/* View Mode Toggle Group - Compact */}
      <div className="flex gap-0 border border-gray-300 dark:border-gray-600 rounded-md overflow-hidden shadow-sm">
        <button
          onClick={() => onModeChange('logs')}
          className={`px-2 py-1 text-xs font-medium transition-colors ${
            currentMode === 'logs' || !currentMode
              ? 'bg-gray-600 dark:bg-gray-500 text-white'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
          }`}
          title="Show logs only"
        >
          📋 Logs
        </button>
        <div className="w-px bg-gray-300 dark:bg-gray-600"></div>
        <button
          onClick={() => onModeChange('split')}
          className={`px-2 py-1 text-xs font-medium transition-colors ${
            currentMode === 'split'
              ? 'bg-gray-600 dark:bg-gray-500 text-white'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
          }`}
          title="Show logs and webview side by side"
        >
          ⚡ Split
        </button>
        <div className="w-px bg-gray-300 dark:bg-gray-600"></div>
        <button
          onClick={() => onModeChange('webview')}
          className={`px-2 py-1 text-xs font-medium transition-colors ${
            currentMode === 'webview'
              ? 'bg-gray-600 dark:bg-gray-500 text-white'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
          }`}
          title="Show webview only"
        >
          🌐 Webview
        </button>
      </div>

      {/* Action Buttons - Grouped with better spacing */}
      <div className="flex gap-1 items-center">
        {config.url && (
          <>
            <button
              onClick={() => onOpenInBrowser(config.url!)}
              className="px-2 py-1 bg-blue-500 text-white rounded text-xs font-medium hover:bg-blue-600 transition-colors shadow-sm"
              title="Open in external browser"
            >
              🌐 Browser
            </button>
            <button
              onClick={() => {
                if (isHttps) {
                  onOpenInTauriWindow(config.url!, config.displayName);
                } else {
                  onOpenInBrowser(config.url!);
                }
              }}
              className="px-2 py-1 bg-green-500 text-white rounded text-xs font-medium hover:bg-green-600 transition-colors shadow-sm"
              title={isHttps 
                ? "Open in Tauri window (handles self-signed certificates)" 
                : "Open in external browser"}
            >
              {isHttps ? '🪟 Window' : '🌐 Open'}
            </button>
          </>
        )}
        <button
          onClick={onMaximize}
          className="px-2 py-1 bg-indigo-500 text-white rounded text-xs font-medium hover:bg-indigo-600 transition-colors shadow-sm"
          title={isMaximized ? "Restore view" : "Maximize view"}
        >
          {isMaximized ? '↗ Restore' : '⛶ Max'}
        </button>
        {hasLogs && (
          <button
            onClick={onClearLogs}
            className="px-2 py-1 bg-red-500 text-white rounded text-xs font-medium hover:bg-red-600 transition-colors shadow-sm"
            title="Clear all logs"
          >
            🗑️ Clear
          </button>
        )}
      </div>
    </div>
  );
}

