import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import { useDeploymentsStore } from '../stores/deploymentsStore';
import { useResourcesStore } from '../stores/resourcesStore';
import type { CommandResponse, CosmosWarning, WhatIfChange } from '../types';
import { type InfrastructureStatus as InfrastructureStatusType } from './InfrastructureStatus';
import ResourceGroupConfig from './ResourceGroupConfig';
import {
  InfrastructureActionsTab,
  InfrastructureConfirmDialogs,
  InfrastructureHistoryTab,
  InfrastructurePanelHeader,
  InfrastructureRecommendedFixesTab,
  InfrastructureResourcesTab,
  InfrastructureTabs,
  InfrastructureTemplatesTab,
} from './infrastructure/components';
import { useCliBuild, useInfrastructureActions, useInfrastructureEnvironment, useResourceGroupConfig, useTemplates, useWorkflowStatus } from './infrastructure/hooks';

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

  const workflowFile = '.start-infrastructure-deploy-dev.yml';
  const repository = 'phoenixvc/Mystira.App';

  const resetState = () => {
    setHasValidated(false);
    setHasPreviewed(false);
    setWhatIfChanges([]);
  };

  const {
    environment,
    showProdConfirm,
    handleEnvironmentChange,
    confirmProdSwitch,
    cancelProdSwitch,
  } = useInfrastructureEnvironment({
    initialEnvironment: 'dev',
    onEnvironmentChanged: () => {},
    onResetState: resetState,
  });

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

  const handleResourceGroupConfirm = async () => {
    await handleDeployConfirmWrapper();
  };

  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-900 p-0">
      <InfrastructureConfirmDialogs
        showDestroyConfirm={showDestroyConfirm}
        showDestroySelect={showDestroySelect}
        showProdConfirm={showProdConfirm}
        showDeployConfirm={showDeployConfirm}
        showResourceGroupConfirm={showResourceGroupConfirm}
        pendingResourceGroup={pendingResourceGroup}
        whatIfChanges={whatIfChanges}
        templates={templates}
        environment={environment}
        onDestroyConfirm={handleDestroyConfirmWrapper}
        onDestroyCancel={() => setShowDestroyConfirm(false)}
        onProdConfirm={() => {
          confirmProdSwitch();
          resetState();
        }}
        onProdCancel={cancelProdSwitch}
        onDeployConfirm={handleDeployConfirmWrapper}
        onDeployCancel={() => setShowDeployConfirm(false)}
        onResourceGroupConfirm={handleResourceGroupConfirm}
        onResourceGroupCancel={() => {
          setShowResourceGroupConfirm(false);
          setPendingResourceGroup(null);
          setLoading(false);
          setCurrentAction(null);
        }}
        onSetLoading={setLoading}
        onSetCurrentAction={setCurrentAction}
        onSetDeploymentProgress={setDeploymentProgress}
        onSetLastResponse={setLastResponse}
        onSetPendingResourceGroup={setPendingResourceGroup}
      />
      
      <div className="p-8 flex-1 flex flex-col min-h-0 w-full">
        <InfrastructurePanelHeader
          environment={environment}
          onEnvironmentChange={handleEnvironmentChange}
          onShowResourceGroupConfig={() => setShowResourceGroupConfig(true)}
          workflowStatus={workflowStatus}
          cliBuildTime={cliBuildTime}
          isBuildingCli={isBuildingCli}
          cliBuildLogs={cliBuildLogs}
          showCliBuildLogs={showCliBuildLogs}
          onShowCliBuildLogs={setShowCliBuildLogs}
          onBuildCli={buildCli}
        />

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
