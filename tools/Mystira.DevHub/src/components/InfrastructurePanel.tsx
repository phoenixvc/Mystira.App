import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import { useResourcesStore } from '../stores/resourcesStore';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import type { CommandResponse, WhatIfChange, WorkflowStatus } from '../types';
import BicepViewer from './BicepViewer';
import WhatIfViewer from './WhatIfViewer';
import ResourceGrid from './ResourceGrid';
import DeploymentHistory from './DeploymentHistory';
import { ConfirmDialog } from './ConfirmDialog';

type Tab = 'actions' | 'bicep' | 'resources' | 'history';

function InfrastructurePanel() {
  const [activeTab, setActiveTab] = useState<Tab>('actions');
  const [loading, setLoading] = useState(false);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [workflowStatus, setWorkflowStatus] = useState<WorkflowStatus | null>(null);
  const [whatIfChanges, setWhatIfChanges] = useState<WhatIfChange[]>([]);
  const [showDestroyConfirm, setShowDestroyConfirm] = useState(false);

  const workflowFile = 'infrastructure-deploy-dev.yml';
  const repository = 'phoenixvc/Mystira.App';

  // Use stores instead of local state
  const {
    resources,
    isLoading: resourcesLoading,
    error: resourcesError,
    fetchResources,
  } = useResourcesStore();

  const {
    deployments,
    isLoading: deploymentsLoading,
    error: deploymentsError,
    fetchDeployments,
  } = useDeploymentsStore();

  // Fetch resources when switching to resources tab
  useEffect(() => {
    if (activeTab === 'resources') {
      fetchResources();
    }
  }, [activeTab, fetchResources]);

  // Fetch deployments when switching to history tab
  useEffect(() => {
    if (activeTab === 'history') {
      fetchDeployments();
    }
  }, [activeTab, fetchDeployments]);

  const handleAction = async (action: 'validate' | 'preview' | 'deploy' | 'destroy') => {
    setLoading(true);
    setLastResponse(null);

    try {
      let response: CommandResponse;

      switch (action) {
        case 'validate':
          response = await invoke('infrastructure_validate', {
            workflowFile,
            repository,
          });
          break;

        case 'preview':
          response = await invoke('infrastructure_preview', {
            workflowFile,
            repository,
          });
          // Mock what-if changes for demonstration
          if (response.success) {
            setWhatIfChanges([
              {
                resourceType: 'Microsoft.DocumentDB/databaseAccounts',
                resourceName: 'dev-euw-cosmos-mystira',
                changeType: 'modify',
                changes: ['consistencyPolicy.defaultConsistencyLevel: BoundedStaleness → Session'],
              },
              {
                resourceType: 'Microsoft.Storage/storageAccounts',
                resourceName: 'deveuwstmystira',
                changeType: 'noChange',
              },
            ]);
          }
          break;

        case 'deploy':
          const confirmDeploy = confirm(
            'Are you sure you want to deploy infrastructure? This will create or update Azure resources.'
          );
          if (!confirmDeploy) {
            setLoading(false);
            return;
          }
          response = await invoke('infrastructure_deploy', {
            workflowFile,
            repository,
          });
          break;

        case 'destroy':
          // This should not be reached directly - destroy should go through confirmation dialog
          response = await invoke('infrastructure_destroy', {
            workflowFile,
            repository,
            confirm: true,
          });
          break;

        default:
          throw new Error(`Unknown action: ${action}`);
      }

      setLastResponse(response);

      // If successful, fetch the workflow status
      if (response.success) {
        setTimeout(() => fetchWorkflowStatus(), 2000);
      }
    } catch (error) {
      setLastResponse({
        success: false,
        error: String(error),
      });
    } finally {
      setLoading(false);
    }
  };

  const fetchWorkflowStatus = async () => {
    try {
      const response: CommandResponse<WorkflowStatus> = await invoke('infrastructure_status', {
        workflowFile,
        repository,
      });

      if (response.success && response.result) {
        setWorkflowStatus(response.result);
      }
    } catch (error) {
      console.error('Failed to fetch workflow status:', error);
    }
  };

  const handleDestroyConfirm = async () => {
    setShowDestroyConfirm(false);
    await handleAction('destroy');
  };

  return (
    <div className="p-8">
      <ConfirmDialog
        isOpen={showDestroyConfirm}
        title="⚠️ Destroy Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirm}
        onCancel={() => setShowDestroyConfirm(false)}
      />
      <div className="max-w-7xl mx-auto">
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            Infrastructure Control Panel
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            Manage Bicep infrastructure deployments via GitHub Actions
          </p>
        </div>

        {/* Tabs */}
        <div className="mb-6">
          <nav className="flex space-x-1 border-b border-gray-200 dark:border-gray-700">
            <button
              onClick={() => setActiveTab('actions')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'actions'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              ⚡ Actions
            </button>
            <button
              onClick={() => setActiveTab('bicep')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'bicep'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              📄 Bicep Templates
            </button>
            <button
              onClick={() => setActiveTab('resources')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'resources'
                  ? 'border-blue-600 dark:border-blue-400 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              ☁️ Azure Resources
            </button>
            <button
              onClick={() => setActiveTab('history')}
              className={`px-4 py-3 text-sm font-medium transition-colors border-b-2 ${
                activeTab === 'history'
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
              }`}
            >
              📜 History
            </button>
          </nav>
        </div>

        {/* Tab Content: Actions */}
        {activeTab === 'actions' && (
          <div>
            {/* Action Buttons */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
              <button
                onClick={() => handleAction('validate')}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white border-2 border-blue-200 rounded-lg hover:border-blue-400 hover:bg-blue-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">🔍</div>
                <div className="text-lg font-semibold text-gray-900">Validate</div>
                <div className="text-sm text-gray-500 text-center mt-1">
                  Check Bicep templates
                </div>
              </button>

              <button
                onClick={() => handleAction('preview')}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white border-2 border-yellow-200 rounded-lg hover:border-yellow-400 hover:bg-yellow-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">👁️</div>
                <div className="text-lg font-semibold text-gray-900">Preview</div>
                <div className="text-sm text-gray-500 text-center mt-1">
                  What-if analysis
                </div>
              </button>

              <button
                onClick={() => handleAction('deploy')}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white border-2 border-green-200 rounded-lg hover:border-green-400 hover:bg-green-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">🚀</div>
                <div className="text-lg font-semibold text-gray-900">Deploy</div>
                <div className="text-sm text-gray-500 text-center mt-1">
                  Deploy infrastructure
                </div>
              </button>

              <button
                onClick={() => setShowDestroyConfirm(true)}
                disabled={loading}
                className="flex flex-col items-center p-6 bg-white dark:bg-gray-800 border-2 border-red-200 dark:border-red-800 rounded-lg hover:border-red-400 dark:hover:border-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <div className="text-4xl mb-2">💥</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">Destroy</div>
                <div className="text-sm text-gray-500 dark:text-gray-400 text-center mt-1">
                  Delete all resources
                </div>
              </button>
            </div>

            {/* Loading State */}
            {loading && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-8">
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 mr-3"></div>
                  <span className="text-blue-800">Executing command...</span>
                </div>
              </div>
            )}

            {/* Response Display */}
            {lastResponse && (
              <div
                className={`rounded-lg p-6 mb-8 ${
                  lastResponse.success
                    ? 'bg-green-50 border border-green-200'
                    : 'bg-red-50 border border-red-200'
                }`}
              >
                <h3
                  className={`text-lg font-semibold mb-2 ${
                    lastResponse.success ? 'text-green-900' : 'text-red-900'
                  }`}
                >
                  {lastResponse.success ? '✅ Success' : '❌ Error'}
                </h3>

                {lastResponse.message && (
                  <p
                    className={`mb-3 ${
                      lastResponse.success ? 'text-green-800' : 'text-red-800'
                    }`}
                  >
                    {lastResponse.message}
                  </p>
                )}

                {lastResponse.error && (
                  <pre className="bg-red-100 p-3 rounded text-sm text-red-900 overflow-auto">
                    {lastResponse.error}
                  </pre>
                )}

                {lastResponse.result !== undefined && lastResponse.result !== null && (
                  <details className="mt-3">
                    <summary
                      className={`cursor-pointer font-medium ${
                        lastResponse.success ? 'text-green-700' : 'text-red-700'
                      }`}
                    >
                      View Details
                    </summary>
                    <pre
                      className={`mt-2 p-3 rounded text-sm overflow-auto ${
                        lastResponse.success
                          ? 'bg-green-100 text-green-900'
                          : 'bg-red-100 text-red-900'
                      }`}
                    >
                      {JSON.stringify(lastResponse.result, null, 2) || 'No details available'}
                    </pre>
                  </details>
                )}
              </div>
            )}

            {/* What-If Viewer */}
            {whatIfChanges.length > 0 && (
              <div className="mb-8">
                <WhatIfViewer changes={whatIfChanges} />
              </div>
            )}

            {/* Workflow Status */}
            {workflowStatus && (
              <div className="bg-white border border-gray-200 rounded-lg p-6 mb-8">
                <h3 className="text-xl font-semibold text-gray-900 mb-4">
                  Workflow Status
                </h3>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                  <div>
                    <div className="text-sm text-gray-500">Status</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.status || 'Unknown'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Conclusion</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.conclusion || 'N/A'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Workflow</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.workflowName || 'N/A'}
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Updated</div>
                    <div className="text-lg font-semibold">
                      {workflowStatus.updatedAt
                        ? new Date(workflowStatus.updatedAt).toLocaleTimeString()
                        : 'N/A'}
                    </div>
                  </div>
                </div>

                {workflowStatus.htmlUrl && (
                  <a
                    href={workflowStatus.htmlUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-block px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    View in GitHub →
                  </a>
                )}

                <button
                  onClick={fetchWorkflowStatus}
                  className="ml-3 inline-block px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition-colors"
                >
                  Refresh Status
                </button>
              </div>
            )}

            {/* Info Box */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 mb-2">ℹ️ Information</h4>
              <ul className="text-sm text-blue-800 space-y-1 list-disc list-inside">
                <li>Workflow: {workflowFile}</li>
                <li>Repository: {repository}</li>
                <li>All actions trigger GitHub Actions workflows</li>
                <li>Requires GitHub CLI to be authenticated</li>
              </ul>
            </div>
          </div>
        )}

        {/* Tab Content: Bicep Viewer */}
        {activeTab === 'bicep' && (
          <div>
            <BicepViewer />
          </div>
        )}

        {/* Tab Content: Azure Resources */}
        {activeTab === 'resources' && (
          <div>
            {resourcesLoading && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-8 text-center">
                <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mb-3"></div>
                <p className="text-blue-800">Loading Azure resources...</p>
              </div>
            )}

            {resourcesError && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-6 mb-4">
                <h3 className="text-lg font-semibold text-red-900 mb-2">❌ Failed to Load Resources</h3>
                <p className="text-red-800 mb-3">{resourcesError}</p>
                <button
                  onClick={() => fetchResources(true)}
                  className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                >
                  Retry
                </button>
              </div>
            )}

            {!resourcesLoading && !resourcesError && (
              <ResourceGrid resources={resources} onRefresh={() => fetchResources(true)} />
            )}
          </div>
        )}

        {/* Tab Content: Deployment History */}
        {activeTab === 'history' && (
          <div>
            {deploymentsLoading && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-8 text-center">
                <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mb-3"></div>
                <p className="text-blue-800">Loading deployment history...</p>
              </div>
            )}

            {deploymentsError && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-6 mb-4">
                <h3 className="text-lg font-semibold text-red-900 mb-2">❌ Failed to Load Deployments</h3>
                <p className="text-red-800 mb-3">{deploymentsError}</p>
                <button
                  onClick={() => fetchDeployments(true)}
                  className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                >
                  Retry
                </button>
              </div>
            )}

            {!deploymentsLoading && !deploymentsError && (
              <DeploymentHistory events={deployments} />
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default InfrastructurePanel;
