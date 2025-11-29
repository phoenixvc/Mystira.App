import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse } from '../../../types';

interface ResponseDisplayProps {
  response: CommandResponse | null;
}

function ResponseDisplay({ response }: ResponseDisplayProps) {
  if (!response) return null;

  const handleInstallAzureCli = async () => {
    try {
      const result = await invoke<CommandResponse>('install_azure_cli');
      if (result.success) {
        alert('Azure CLI installation started. Please restart the application after installation completes.');
      } else {
        alert(`Failed to install Azure CLI: ${result.error || 'Unknown error'}`);
      }
    } catch (error) {
      alert(`Error installing Azure CLI: ${error}`);
    }
  };

  const showInstallButton = response.error && (
    response.error.includes('Azure CLI is not installed') ||
    response.error.includes('Azure CLI not found')
  );

  return (
    <div
      className={`rounded-lg p-6 mb-8 ${
        response.success
          ? 'bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800'
          : 'bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800'
      }`}
    >
      <h3
        className={`text-lg font-semibold mb-2 ${
          response.success ? 'text-green-900 dark:text-green-300' : 'text-red-900 dark:text-red-300'
        }`}
      >
        {response.success ? '✅ Success' : '❌ Error'}
      </h3>

      {response.message && (
        <p
          className={`mb-3 ${
            response.success ? 'text-green-800 dark:text-green-200' : 'text-red-800 dark:text-red-200'
          }`}
        >
          {response.message}
        </p>
      )}

      {response.error && (
        <div>
          <pre className="bg-red-100 dark:bg-red-900/50 p-3 rounded text-sm text-red-900 dark:text-red-200 overflow-auto mb-3">
            {response.error}
          </pre>
          {showInstallButton && (
            <button
              onClick={handleInstallAzureCli}
              className="px-4 py-2 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors"
            >
              📦 Install Azure CLI
            </button>
          )}
        </div>
      )}

      {response.result !== undefined && response.result !== null && (
        <details className="mt-3">
          <summary
            className={`cursor-pointer font-medium ${
              response.success ? 'text-green-700 dark:text-green-300' : 'text-red-700 dark:text-red-300'
            }`}
          >
            View Details
          </summary>
          <pre
            className={`mt-2 p-3 rounded text-sm overflow-auto ${
              response.success
                ? 'bg-green-100 dark:bg-green-900/50 text-green-900 dark:text-green-200'
                : 'bg-red-100 dark:bg-red-900/50 text-red-900 dark:text-red-200'
            }`}
          >
            {JSON.stringify(response.result, null, 2) || 'No details available'}
          </pre>
        </details>
      )}
    </div>
  );
}

export default ResponseDisplay;
