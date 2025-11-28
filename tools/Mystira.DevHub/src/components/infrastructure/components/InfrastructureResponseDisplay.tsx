import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse, CosmosWarning, WhatIfChange } from '../../../types';

interface InfrastructureResponseDisplayProps {
  response: CommandResponse | null;
  cosmosWarning?: CosmosWarning | null;
  onCosmosWarningChange?: (warning: CosmosWarning | null) => void;
  whatIfChanges?: WhatIfChange[];
  onLastResponseChange?: (response: CommandResponse | null) => void;
}

export function InfrastructureResponseDisplay({ 
  response,
  cosmosWarning,
  onCosmosWarningChange,
  whatIfChanges = [],
  onLastResponseChange,
}: InfrastructureResponseDisplayProps) {
  const handleInstallAzureCli = async () => {
    try {
      const installResponse = await invoke<CommandResponse>('install_azure_cli');
      if (installResponse.success) {
        alert('Azure CLI installation started. Please restart the application after installation completes.');
      } else {
        alert(`Failed to install Azure CLI: ${installResponse.error || 'Unknown error'}`);
      }
    } catch (error) {
      alert(`Error installing Azure CLI: ${error}`);
    }
  };

  if (!response) {
    return null;
  }

  const hasAzureCliError = response.error && (
    response.error.includes('Azure CLI is not installed') ||
    response.error.includes('Azure CLI not found')
  );

  return (
    <>
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
          {hasAzureCliError && (
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
    
    {/* Cosmos DB Warning Banner - Dismissible */}
    {cosmosWarning && !cosmosWarning.dismissed && (
      <div className="rounded-lg p-4 mb-6 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700">
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-3">
            <span className="text-amber-500 text-xl">⚠️</span>
            <div className="flex-1">
              <h4 className="text-sm font-semibold text-amber-800 dark:text-amber-200 mb-1">
                Expected Cosmos DB Preview Warnings
              </h4>
              <p className="text-xs text-amber-700 dark:text-amber-300 mb-2">
                Azure's what-if preview cannot predict changes for nested Cosmos DB resources
                (databases and containers) that don't exist yet. This is a known Azure limitation
                and does not affect actual deployments.
              </p>
              {cosmosWarning.affectedResources.length > 0 && (
                <div className="text-xs text-amber-600 dark:text-amber-400 mb-2">
                  <span className="font-medium">Affected resources:</span>{' '}
                  {cosmosWarning.affectedResources.join(', ')}
                </div>
              )}
              <details className="text-xs">
                <summary className="cursor-pointer text-amber-600 dark:text-amber-400 hover:text-amber-800 dark:hover:text-amber-200">
                  View full error details
                </summary>
                <pre className="mt-2 p-2 bg-amber-100 dark:bg-amber-900/50 rounded text-[10px] overflow-auto max-h-32 text-amber-800 dark:text-amber-200">
                  {cosmosWarning.details}
                </pre>
              </details>
            </div>
          </div>
          <button
            onClick={() => {
              if (onCosmosWarningChange) {
                onCosmosWarningChange({ ...cosmosWarning, dismissed: true });
              }
              if (onLastResponseChange) {
                onLastResponseChange({
                  success: true,
                  message: `Preview completed. ${whatIfChanges.length} resource changes ready for deployment.`,
                });
              }
            }}
            className="ml-4 px-3 py-1.5 text-xs font-medium bg-amber-100 dark:bg-amber-800 hover:bg-amber-200 dark:hover:bg-amber-700 text-amber-700 dark:text-amber-200 rounded-md transition-colors whitespace-nowrap"
          >
            Dismiss & Continue
          </button>
        </div>
      </div>
    )}
    </>
  );
}

