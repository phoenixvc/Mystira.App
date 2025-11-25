import { ServiceConfig } from './types';

interface WebviewViewProps {
  config: ServiceConfig;
  hasError: boolean;
  isMaximized: boolean;
  containerClass: string;
  onRetry: () => void;
  onOpenInTauriWindow: (url: string, title: string) => void;
  onOpenInBrowser: (url: string) => void;
  onError: () => void;
}

export function WebviewView({
  config,
  hasError,
  isMaximized,
  containerClass,
  onRetry,
  onOpenInTauriWindow,
  onOpenInBrowser,
  onError,
}: WebviewViewProps) {
  const isHttps = config.url?.startsWith('https://');
  
  // For HTTPS URLs, show a button to open in Tauri window instead of iframe
  if (isHttps) {
    return (
      <div className={`flex flex-col items-center justify-center h-full p-8 bg-gray-50 dark:bg-gray-900 text-center ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
        <div className="max-w-md">
          <div className="text-blue-500 text-5xl mb-4">🔒</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
            Open {config.displayName} in Secure Window
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
            This service uses HTTPS with a self-signed certificate. Click the button below to open it in a Tauri window where you can accept the certificate.
          </p>
          <div className="flex flex-col gap-3">
            <button
              onClick={() => onOpenInTauriWindow(config.url!, config.displayName)}
              className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 font-medium text-base shadow-lg transition-colors"
            >
              🪟 Open in Tauri Window
            </button>
            <button
              onClick={() => onOpenInBrowser(config.url!)}
              className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 text-sm transition-colors"
            >
              Or open in external browser
            </button>
          </div>
        </div>
      </div>
    );
  }
  
  // For HTTP URLs, use iframe as normal
  return (
    <div className={`flex flex-col ${isMaximized ? 'h-full flex-1 min-h-0' : containerClass}`}>
      {hasError ? (
        <div className="flex flex-col items-center justify-center h-full p-8 bg-gray-50 dark:bg-gray-900 text-center">
          <div className="text-red-500 text-4xl mb-4">⚠️</div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
            Unable to connect to {config.displayName}
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-4 max-w-md">
            The webview cannot connect to {config.url}. This might be due to:
          </p>
          <ul className="text-sm text-gray-600 dark:text-gray-400 mb-6 text-left max-w-md list-disc list-inside">
            <li>Service not fully started yet</li>
            <li>CORS or security restrictions</li>
          </ul>
          <div className="flex gap-2">
            <button
              onClick={onRetry}
              className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
            >
              Retry
            </button>
            <button
              onClick={() => onOpenInTauriWindow(config.url!, config.displayName)}
              className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
            >
              Open in Tauri Window
            </button>
          </div>
        </div>
      ) : (
        <iframe
          key={`webview-${config.name}`}
          src={config.url || ''}
          className="w-full flex-1 border-0 min-h-0"
          title={`${config.displayName} Webview`}
          sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-modals"
          onError={onError}
        />
      )}
    </div>
  );
}

