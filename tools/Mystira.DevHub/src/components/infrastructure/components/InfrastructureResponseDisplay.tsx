import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import type { CommandResponse, CosmosWarning, WhatIfChange } from '../../../types';
import { ConfirmDialog } from '../../ConfirmDialog';

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
  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [pendingResourceId, setPendingResourceId] = useState<string | null>(null);
  // Send errors to Problems tab
  useEffect(() => {
    if (response?.error) {
      const event = new CustomEvent('infrastructure-problem', {
        detail: {
          severity: 'error' as const,
          message: response.error,
          source: 'Infrastructure',
          details: response.result ? JSON.stringify(response.result, null, 2) : undefined,
        },
      });
      window.dispatchEvent(event);
    }
  }, [response?.error, response?.result]);

  // Send warnings to Problems tab
  useEffect(() => {
    if (cosmosWarning && !cosmosWarning.dismissed) {
      const event = new CustomEvent('infrastructure-problem', {
        detail: {
          severity: 'warning' as const,
          message: `Cosmos DB Preview Warnings: ${cosmosWarning.message}`,
          source: 'Infrastructure',
          details: cosmosWarning.details,
        },
      });
      window.dispatchEvent(event);
    }
  }, [cosmosWarning]);

  // Extract resource ID and name from error message for failed provisioning state
  const getFailedResourceInfo = (error: string): { resourceId: string; resourceName: string } | null => {
    // Try to extract from the target field in JSON error
    let resourceId: string | null = null;
    const targetMatch = error.match(/"target":\s*"([^"]+databaseAccounts[^"]+)"/);
    if (targetMatch && targetMatch[1]) {
      resourceId = targetMatch[1];
    } else {
      // Try to extract from the error message directly
      const resourceIdMatch = error.match(/\/subscriptions\/[^\/]+\/resourceGroups\/[^\/]+\/providers\/Microsoft\.DocumentDB\/databaseAccounts\/[a-zA-Z0-9-]+/);
      if (resourceIdMatch && resourceIdMatch[0]) {
        resourceId = resourceIdMatch[0];
      }
    }
    
    if (!resourceId) return null;
    
    // Extract resource name from resource ID (last segment after databaseAccounts/)
    const nameMatch = resourceId.match(/databaseAccounts\/([a-zA-Z0-9-]+)/);
    const resourceName = nameMatch ? nameMatch[1] : 'Unknown';
    
    return { resourceId, resourceName };
  };

  const handleDeleteFailedResourceClick = () => {
    if (!response?.error) return;
    
    const resourceInfo = getFailedResourceInfo(response.error);
    if (!resourceInfo) {
      alert('Could not extract resource information from error message.');
      return;
    }
    
    setPendingResourceId(resourceInfo.resourceId);
    setShowDeleteConfirm(true);
  };

  const handleDeleteConfirm = async () => {
    if (!pendingResourceId) return;
    
    setShowDeleteConfirm(false);
    setIsDeleting(true);
    try {
      const result = await invoke<CommandResponse>('delete_azure_resource', {
        resourceId: pendingResourceId,
      });
      
      if (result.success) {
        alert(`✅ Successfully deleted the failed Cosmos DB account.\n\nYou can now retry the deployment.`);
        // Refresh the response to clear the error
        if (onLastResponseChange) {
          onLastResponseChange({
            success: true,
            message: 'Failed resource deleted successfully. You can now retry deployment.',
          });
        }
      } else {
        alert(`❌ Failed to delete resource: ${result.error || 'Unknown error'}`);
      }
    } catch (error) {
      alert(`❌ Error deleting resource: ${String(error)}`);
    } finally {
      setIsDeleting(false);
      setPendingResourceId(null);
    }
  };

  const hasFailedProvisioningState = response?.error?.includes('failed provisioning state') || false;
  const failedResourceInfo = response?.error ? getFailedResourceInfo(response.error) : null;

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
    {/* Cosmos DB Warning Banner - Dismissible - Show FIRST so it's always visible */}
    {cosmosWarning && !cosmosWarning.dismissed && (
      <div className="rounded-lg p-4 mb-6 bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-3 flex-1 min-w-0">
            <span className="text-amber-500 text-xl flex-shrink-0">⚠️</span>
            <div className="flex-1 min-w-0">
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
            className="px-3 py-1.5 text-xs font-medium bg-amber-100 dark:bg-amber-800 hover:bg-amber-200 dark:hover:bg-amber-700 text-amber-700 dark:text-amber-200 rounded-md transition-colors whitespace-nowrap flex-shrink-0"
          >
            Dismiss & Continue
          </button>
        </div>
      </div>
    )}
    
    {/* Warnings Display - Show warnings separately if they exist in result */}
    {response.success && response.result && (response.result as any).warnings && (
      <div className="rounded-lg p-4 mb-4 bg-yellow-50 dark:bg-yellow-900/30 border border-yellow-200 dark:border-yellow-800">
        <div className="flex items-start gap-3">
          <span className="text-yellow-500 text-xl flex-shrink-0">⚠️</span>
          <div className="flex-1">
            <h4 className="text-sm font-semibold text-yellow-800 dark:text-yellow-200 mb-2">
              Validation Warnings
            </h4>
            <p className="text-xs text-yellow-700 dark:text-yellow-300 mb-2">
              These are informational warnings and do not prevent deployment. You can safely proceed.
            </p>
            <details className="text-xs">
              <summary className="cursor-pointer text-yellow-600 dark:text-yellow-400 hover:text-yellow-800 dark:hover:text-yellow-200">
                View warning details
              </summary>
              <pre className="mt-2 p-2 bg-yellow-100 dark:bg-yellow-900/50 rounded text-[10px] overflow-auto max-h-48 text-yellow-800 dark:text-yellow-200">
                {(response.result as any).warnings}
              </pre>
            </details>
          </div>
        </div>
      </div>
    )}
    
    {/* Response Display - Show AFTER warning banner */}
    <div
      className={`rounded-lg p-4 mb-4 ${
        response.success
          ? 'bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800'
          : 'bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800'
      }`}
    >
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <h3
            className={`text-sm font-semibold mb-1 ${
              response.success ? 'text-green-900 dark:text-green-300' : 'text-red-900 dark:text-red-300'
            }`}
          >
            {response.success ? '✅ Success' : '❌ Error'}
          </h3>

          {response.message && (
            <p
              className={`text-xs ${
                response.success ? 'text-green-800 dark:text-green-200' : 'text-red-800 dark:text-red-200'
              }`}
            >
              {response.message}
            </p>
          )}

          {response.error && (
            <div className="mt-2">
              <p className="text-xs text-red-800 dark:text-red-200 mb-2">
                Error details have been sent to the <strong>Problems</strong> tab in the bottom panel.
              </p>
              <div className="flex flex-wrap gap-2">
                {hasAzureCliError && (
                  <button
                    onClick={handleInstallAzureCli}
                    className="px-3 py-1.5 text-xs bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 transition-colors"
                  >
                    📦 Install Azure CLI
                  </button>
                )}
                {hasFailedProvisioningState && failedResourceInfo && (
                  <button
                    onClick={handleDeleteFailedResourceClick}
                    disabled={isDeleting}
                    className="px-3 py-1.5 text-xs bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-1"
                  >
                    {isDeleting ? (
                      <>
                        <div className="animate-spin rounded-full h-3 w-3 border-b-2 border-white"></div>
                        <span>Deleting...</span>
                      </>
                    ) : (
                      <>
                        <span>🗑️</span>
                        <span>Delete Failed Resource</span>
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
    
    {/* Delete Confirmation Dialog */}
    <ConfirmDialog
      isOpen={showDeleteConfirm}
      title="🗑️ Delete Failed Cosmos DB Account"
      message={failedResourceInfo ? `You are about to permanently delete the failed Cosmos DB account.\n\nResource Name: ${failedResourceInfo.resourceName}\nResource ID: ${failedResourceInfo.resourceId}\n\n⚠️ This action cannot be undone.` : ''}
      confirmText="Yes, Delete"
      cancelText="Cancel"
      confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
      requireTextMatch="DELETE"
      onConfirm={handleDeleteConfirm}
      onCancel={() => {
        setShowDeleteConfirm(false);
        setPendingResourceId(null);
      }}
    />
    </>
  );
}

