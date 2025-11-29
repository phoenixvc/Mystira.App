import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, CosmosWarning, ResourceGroupConvention, TemplateConfig, WhatIfChange, WorkflowStatus } from '../types';
import { type ProjectInfo } from '../types';
import { ConfirmDialog } from './ConfirmDialog';
import { type InfrastructureStatus as InfrastructureStatusType } from './InfrastructureStatus';
import ResourceGroupConfig from './ResourceGroupConfig';
import {
  CliBuildLogsViewer,
  InfrastructureActionsTab,
  InfrastructureHistoryTab,
  InfrastructureRecommendedFixesTab,
  InfrastructureResourcesTab,
  InfrastructureTabs,
  InfrastructureTemplatesTab,
} from './infrastructure/components';
import { useCliBuild, useInfrastructureActions, useResourceGroupConfig, useTemplates, useWorkflowStatus } from './infrastructure/hooks';
import { formatTimeSince } from './services/utils/serviceUtils';

type Tab = 'actions' | 'templates' | 'resources' | 'history' | 'recommended-fixes';

function InfrastructurePanel() {
  const [activeTab, setActiveTab] = useState<Tab>('actions');
  const [loading, setLoading] = useState(false);
  const [currentAction, setCurrentAction] = useState<'validate' | 'preview' | 'deploy' | 'destroy' | null>(null);
  const [lastResponse, setLastResponse] = useState<CommandResponse | null>(null);
  const [whatIfChanges, setWhatIfChanges] = useState<WhatIfChange[]>([]);
  const [showDestroyConfirm, setShowDestroyConfirm] = useState(false);
  const deploymentMethod: 'github' | 'azure-cli' = 'azure-cli';
  const [repoRoot, setRepoRoot] = useState<string>('');
  const [environment, setEnvironment] = useState<string>('dev');
  const [showProdConfirm, setShowProdConfirm] = useState(false);
  const [pendingEnvironment, setPendingEnvironment] = useState<string>('dev');
  const [hasValidated, setHasValidated] = useState(false);
  const [hasPreviewed, setHasPreviewed] = useState(false);
  const [hasDeployedInfrastructure, setHasDeployedInfrastructure] = useState(false);
  const [showDeployConfirm, setShowDeployConfirm] = useState(false);
  const [showOutputPanel, setShowOutputPanel] = useState(false);
  const [deploymentProgress, setDeploymentProgress] = useState<string | null>(null);
  const [showResourceGroupConfirm, setShowResourceGroupConfirm] = useState(false);
  const [pendingResourceGroup, setPendingResourceGroup] = useState<{ resourceGroup: string; location: string } | null>(null);
  const [showDestroySelect, setShowDestroySelect] = useState(false);
  const [showResourceGroupConfig, setShowResourceGroupConfig] = useState(false);
  const [step1Collapsed, setStep1Collapsed] = useState(false);
  const [showStep2, setShowStep2] = useState(false);
  const [infrastructureLoading, setInfrastructureLoading] = useState(true);
  const [cosmosWarning, setCosmosWarning] = useState<CosmosWarning | null>(null);
  const [fetchingResourceGroup, setFetchingResourceGroup] = useState(false);
  const [storageAccountConflict] = useState<StorageAccountConflictWarning | null>(null);
  const [deletingStorageAccount, setDeletingStorageAccount] = useState(false);
  const [showDeleteStorageConfirm, setShowDeleteStorageConfirm] = useState(false);
  const [autoRetryAfterDelete, setAutoRetryAfterDelete] = useState(false);
  const [fetchingResourceGroup] = useState(false);

  const workflowFile = '.start-infrastructure-deploy-dev.yml';
  const repository = 'phoenixvc/Mystira.App';

  const { templates, setTemplates } = useTemplates(environment);
  const { config: resourceGroupConfig, setConfig: setResourceGroupConfig } = useResourceGroupConfig(environment);
  const { status: workflowStatus, fetchStatus: fetchWorkflowStatus } = useWorkflowStatus(workflowFile, repository);
  const { isBuilding: isBuildingCli, buildTime: cliBuildTime, logs: cliBuildLogs, showLogs: showCliBuildLogs, setShowLogs: setShowCliBuildLogs, build: buildCli } = useCliBuild();

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

  useEffect(() => {
    const fetchRepoRoot = async () => {
      try {
        const root = await invoke<string>('get_repo_root');
        setRepoRoot(root);
      } catch (error) {
        console.error('Failed to get repo root:', error);
      }
    };
    fetchRepoRoot();
  }, []);

  useEffect(() => {
    if (activeTab === 'resources') {
      fetchResources(false, environment);
    }
  }, [activeTab, environment, fetchResources]);

  useEffect(() => {
    if (activeTab === 'history') {
      fetchDeployments();
    }
  }, [activeTab, fetchDeployments]);

  const { handleAction: handleActionFromHook, handleDestroyConfirm, handleDeployConfirm } = useInfrastructureActions({
    deploymentMethod,
    repoRoot,
    environment,
    templates,
    resourceGroupConfig,
    hasValidated,
    hasPreviewed,
    whatIfChanges,
    cosmosWarning,
    workflowFile,
    repository,
    onSetLoading: setLoading,
    onSetLastResponse: setLastResponse,
    onSetShowOutputPanel: setShowOutputPanel,
    onSetHasValidated: setHasValidated,
    onSetHasPreviewed: setHasPreviewed,
    onSetWhatIfChanges: setWhatIfChanges,
    onSetCosmosWarning: setCosmosWarning,
    onSetShowDeployConfirm: setShowDeployConfirm,
    onSetShowDestroySelect: setShowDestroySelect,
    onFetchWorkflowStatus: fetchWorkflowStatus,
    onSetCurrentAction: setCurrentAction,
    onSetHasDeployedInfrastructure: setHasDeployedInfrastructure,
    onSetDeploymentProgress: setDeploymentProgress,
    onSetShowResourceGroupConfirm: (show: boolean, resourceGroup?: string, location?: string) => {
      if (show && resourceGroup && location) {
        setPendingResourceGroup({ resourceGroup, location });
        setShowResourceGroupConfirm(true);
      } else {
        setShowResourceGroupConfirm(false);
        setPendingResourceGroup(null);
      }
    },
  });

  const handleAction = async (action: 'validate' | 'preview' | 'deploy' | 'destroy') => {
    await handleActionFromHook(action);
  };

  const handleDeployConfirmWrapper = async () => {
    setShowDeployConfirm(false);
    await handleDeployConfirm(async () => {
      try {
        const resourceGroup = resourceGroupConfig.defaultResourceGroup || `dev-san-rg-mystira-app`;
        const statusResponse = await invoke<any>('check_infrastructure_status', {
          environment,
          resourceGroup,
        });
        if (statusResponse.success && statusResponse.result) {
          const status = statusResponse.result as InfrastructureStatusType;
          setHasDeployedInfrastructure(status.available);
        }
      } catch (error) {
        console.error('Failed to refresh infrastructure status:', error);
      }
    });
  };

  const handleDestroyConfirmWrapper = async () => {
    setShowDestroyConfirm(false);
    setShowDestroySelect(false);
    await handleDestroyConfirm();
  };

  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-900 p-0">
      <ConfirmDialog
        isOpen={showDestroyConfirm && !showDestroySelect}
        title="⚠️ Destroy All Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirmWrapper}
        onCancel={() => setShowDestroyConfirm(false)}
      />
      <ConfirmDialog
        isOpen={showProdConfirm}
        title="⚠️ Production Environment Warning"
        message="You are about to switch to the PRODUCTION environment. All operations (validate, preview, deploy, destroy) will affect production resources. This is a critical environment with real users and data. Are you absolutely sure you want to proceed?"
        confirmText="Yes, Switch to Production"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="PRODUCTION"
        onConfirm={() => {
          setEnvironment(pendingEnvironment);
          setShowProdConfirm(false);
          setHasValidated(false);
          setHasPreviewed(false);
          setWhatIfChanges([]);
        }}
        onCancel={() => {
          setShowProdConfirm(false);
          setPendingEnvironment(environment);
        }}
      />
      <ConfirmDialog
        isOpen={showDestroySelect}
        title="💥 Destroy Selected Resources"
        message={`⚠️ WARNING: You are about to permanently DELETE ${whatIfChanges.filter(c => c.selected !== false && (c.changeType === 'delete' || c.selected === true)).length} selected resource(s) from Azure.\n\nThis action CANNOT be undone and will permanently remove:\n${whatIfChanges.filter(c => c.selected !== false && (c.changeType === 'delete' || c.selected === true)).map(c => `  • ${c.resourceName} (${c.resourceType})`).join('\n')}\n\nType "DELETE" in the field below to confirm.`}
        confirmText="Yes, Destroy Selected"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="DELETE"
        onConfirm={handleDestroyConfirmWrapper}
        onCancel={() => setShowDestroySelect(false)}
      />
      <ConfirmDialog
        isOpen={showDeployConfirm}
        title="🚀 Deploy Selected Resources"
        message={`You are about to deploy ${whatIfChanges.length > 0 
          ? whatIfChanges.filter(c => c.selected !== false).length 
          : templates.filter(t => t.selected).length} ${whatIfChanges.length > 0 ? 'selected resource(s)' : 'template(s)'} to ${environment} environment.`}
        confirmText="Deploy Selected Resources"
        cancelText="Cancel"
        confirmButtonClass="bg-green-600 hover:bg-green-700"
        onConfirm={handleDeployConfirmWrapper}
        onCancel={() => setShowDeployConfirm(false)}
      />
      <ConfirmDialog
        isOpen={showResourceGroupConfirm}
        title="📦 Create Resource Group"
        message={pendingResourceGroup 
          ? `The resource group "${pendingResourceGroup.resourceGroup}" does not exist in location "${pendingResourceGroup.location}".\n\nWould you like to create it now? This is required before deploying infrastructure.`
          : ''}
        confirmText="Create Resource Group"
        cancelText="Cancel"
        confirmButtonClass="bg-blue-600 hover:bg-blue-700"
        onConfirm={async () => {
          if (pendingResourceGroup) {
            setShowResourceGroupConfirm(false);
            setLoading(true);
            setCurrentAction('deploy');
            setDeploymentProgress(`Creating resource group '${pendingResourceGroup.resourceGroup}'...`);
            
            try {
              const { invoke } = await import('@tauri-apps/api/tauri');
              const createRgResponse = await invoke<any>('azure_create_resource_group', {
                resourceGroup: pendingResourceGroup.resourceGroup,
                location: pendingResourceGroup.location,
              });
              
              if (!createRgResponse.success) {
                setLastResponse({
                  success: false,
                  error: createRgResponse.error || `Failed to create resource group '${pendingResourceGroup.resourceGroup}'`,
                });
                setLoading(false);
                setCurrentAction(null);
                setPendingResourceGroup(null);
                return;
              }
              
              // Retry deployment after creating resource group
              setDeploymentProgress(`Deploying to ${pendingResourceGroup.resourceGroup}...`);
              await handleDeployConfirmWrapper();
            } catch (error) {
              setLastResponse({
                success: false,
                error: `Failed to create resource group: ${String(error)}`,
              });
              setLoading(false);
              setCurrentAction(null);
            } finally {
              setPendingResourceGroup(null);
            }
          }
        }}
        onCancel={() => {
          setShowResourceGroupConfirm(false);
          setPendingResourceGroup(null);
          setLoading(false);
          setCurrentAction(null);
        }}
      />
      <div className="p-8 flex-1 flex flex-col min-h-0 w-full">
        <div className="mb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white">
                Infrastructure Control Panel
              </h2>
              <div className="flex items-center gap-2">
                <label className="text-sm text-gray-600 dark:text-gray-400">Environment:</label>
                <select
                  value={environment}
                  aria-label="Select environment"
                  onChange={async (e) => {
                    const newEnv = e.target.value;
                    if (newEnv === 'prod') {
                      try {
                        const ownerCheck = await invoke<CommandResponse<{ isOwner: boolean; userName: string }>>('check_subscription_owner');
                        if (ownerCheck.success && ownerCheck.result?.isOwner) {
                          setPendingEnvironment(newEnv);
                          setShowProdConfirm(true);
                        } else {
                          alert('Access Denied: You must have Subscription Owner role to switch to production environment.\n\n' +
                                `Current user: ${ownerCheck.result?.userName || 'Unknown'}\n` +
                                'Please contact your subscription administrator.');
                          e.target.value = environment;
                        }
                      } catch (error) {
                        console.error('Failed to check subscription owner:', error);
                        alert('Failed to verify subscription owner role. Cannot switch to production environment.');
                        e.target.value = environment;
                      }
                    } else {
                      setEnvironment(newEnv);
                      setHasValidated(false);
                      setHasPreviewed(false);
                      setWhatIfChanges([]);
                    }
                  }}
                  className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
                >
                  <option value="dev">dev</option>
                  <option value="staging">staging</option>
                  <option value="prod">prod</option>
                </select>
                <button
                  onClick={() => setShowResourceGroupConfig(true)}
                  className="px-4 py-2 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-lg text-sm font-medium"
                  title="Configure resource group naming conventions"
                >
                  ⚙️ Resource Groups
                </button>
              </div>
            </div>
            <div className="flex flex-col items-end gap-2">
              {workflowStatus?.updatedAt && (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last Workflow Build</div>
                  <div className="px-3 py-1.5 rounded-lg bg-blue-900/20 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-mono font-semibold text-sm" 
                       title={`Last workflow build: ${new Date(workflowStatus.updatedAt).toLocaleString()}`}>
                    {formatTimeSince(new Date(workflowStatus.updatedAt).getTime()) || 'Unknown'}
                  </div>
                </div>
              )}
              {cliBuildTime ? (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Last CLI Build</div>
                  <div className="flex items-center gap-2">
                    <div className="px-3 py-1.5 rounded-lg bg-green-900/20 dark:bg-green-900/30 text-green-600 dark:text-green-400 font-mono font-semibold text-sm" 
                         title={`Last CLI build: ${new Date(cliBuildTime).toLocaleString()}`}>
                      {formatTimeSince(cliBuildTime) || 'Unknown'}
                    </div>
                    <button
                      onClick={() => {
                        setShowCliBuildLogs(true);
                        buildCli();
                      }}
                      disabled={isBuildingCli}
                      className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5"
                      title="Rebuild the CLI executable"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                          Building...
                        </>
                      ) : (
                        <>🔨 Rebuild</>
                      )}
                    </button>
                  </div>
                </div>
              ) : (
                <div className="flex flex-col items-end">
                  <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">CLI Status</div>
                  <div className="flex items-center gap-2">
                    <div className="px-3 py-1.5 rounded-lg bg-red-900/20 dark:bg-red-900/30 text-red-600 dark:text-red-400 font-mono font-semibold text-sm">
                      Not Built
                    </div>
                    <button
                      onClick={() => {
                        setShowCliBuildLogs(true);
                        buildCli();
                      }}
                      disabled={isBuildingCli}
                      className="px-3 py-1.5 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-1.5"
                      title="Build the CLI executable"
                    >
                      {isBuildingCli ? (
                        <>
                          <span className="inline-block animate-spin rounded-full h-3 w-3 border-b-2 border-white"></span>
                          Building...
                        </>
                      ) : (
                        <>🔨 Build CLI</>
                      )}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {showCliBuildLogs && (
          <div className="mb-6">
            <CliBuildLogsViewer
              isBuilding={isBuildingCli}
              logs={cliBuildLogs}
              showLogs={showCliBuildLogs}
              onClose={() => setShowCliBuildLogs(false)}
            />
          </div>
        )}

        <InfrastructureTabs activeTab={activeTab} onTabChange={setActiveTab} />

        {activeTab === 'actions' && (
          <InfrastructureActionsTab
            environment={environment}
            templates={templates}
            onTemplatesChange={setTemplates}
            resourceGroupConfig={resourceGroupConfig}
            onResourceGroupConfigChange={setResourceGroupConfig}
            step1Collapsed={step1Collapsed}
            onStep1CollapsedChange={setStep1Collapsed}
            showStep2={showStep2}
            onShowStep2Change={setShowStep2}
            hasValidated={hasValidated}
            hasPreviewed={hasPreviewed}
            loading={loading}
            currentAction={currentAction}
            onAction={handleAction}
            lastResponse={lastResponse}
            whatIfChanges={whatIfChanges}
            onWhatIfChangesChange={setWhatIfChanges}
            cosmosWarning={cosmosWarning}
            onCosmosWarningChange={setCosmosWarning}
            infrastructureLoading={infrastructureLoading}
            onInfrastructureLoadingChange={setInfrastructureLoading}
            workflowStatus={workflowStatus}
            deploymentMethod={deploymentMethod}
            onShowDestroySelect={() => setShowDestroySelect(true)}
            hasDeployedInfrastructure={hasDeployedInfrastructure}
            deploymentProgress={deploymentProgress}
          />
        )}

        {activeTab === 'templates' && (
          <InfrastructureTemplatesTab environment={environment} />
        )}

        {activeTab === 'resources' && (
          <InfrastructureResourcesTab
            environment={environment}
            resources={resources}
            resourcesLoading={resourcesLoading}
            resourcesError={resourcesError}
            onFetchResources={fetchResources}
          />
        )}

        {activeTab === 'history' && (
          <InfrastructureHistoryTab
            deployments={deployments}
            deploymentsLoading={deploymentsLoading}
            deploymentsError={deploymentsError}
            onFetchDeployments={fetchDeployments}
          />
        )}

        {activeTab === 'recommended-fixes' && (
          <InfrastructureRecommendedFixesTab environment={environment} />
        )}

        {showResourceGroupConfig && (
          <ResourceGroupConfig
            environment={environment}
            onSave={(config) => {
              setResourceGroupConfig(config);
              const updated = whatIfChanges.map(change => ({
                ...change,
                resourceGroup: change.resourceGroup || 
                  config.resourceTypeMappings?.[change.resourceType] || 
                  config.defaultResourceGroup,
              }));
              setWhatIfChanges(updated);
              setShowResourceGroupConfig(false);
            }}
            onClose={() => setShowResourceGroupConfig(false)}
          />
        )}
      </div>

      {!showOutputPanel && lastResponse && (
        <button
          onClick={() => setShowOutputPanel(true)}
          className={`px-4 py-2 text-xs border-t border-gray-200 dark:border-gray-700 flex items-center gap-2 ${
            lastResponse.success
              ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300'
              : 'bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300'
          }`}
        >
          <span>{lastResponse.success ? '✓' : '✕'}</span>
          <span>
            {lastResponse.success
              ? (lastResponse.message || 'Operation completed')
              : (lastResponse.error || 'Operation failed')}
          </span>
          <span className="ml-auto text-gray-400">Click to expand</span>
        </button>
      )}
    </div>
  );
}

export default InfrastructurePanel;

