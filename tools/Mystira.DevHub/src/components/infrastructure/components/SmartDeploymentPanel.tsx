import { useState, useEffect } from 'react';
import {
  Play,
  Square,
  SkipForward,
  RefreshCw,
  ChevronUp,
  ChevronDown,
  CheckCircle2,
  XCircle,
  Loader2,
  Clock,
  AlertTriangle,
  Globe,
  Server,
  Copy,
  Check,
  Rocket,
  Settings2,
} from 'lucide-react';
import {
  useSmartDeploymentStore,
  AZURE_REGIONS,
  type RegionId,
  type RegionStatus,
} from '../../../stores/smartDeploymentStore';
import {
  PrerequisitesCheckPanel,
  ResourceDiscoveryPanel,
  SmartDeployDecision,
  GitOperationsPanel,
  ConfigurationPanel,
  type ResourceGroup,
  type StaticWebApp,
  type DiscoveredResources,
  type DeployMode,
} from './deploy-now';

interface SmartDeploymentPanelProps {
  repoRoot: string;
}

export function SmartDeploymentPanel({ repoRoot }: SmartDeploymentPanelProps) {
  const {
    regionPriority,
    environment,
    isDeploying,
    isCancelled,
    currentRegionIndex,
    regionStatuses,
    deploymentResult,
    allAttemptsFailed,
    logs,
    setRegionPriority,
    moveRegionUp,
    moveRegionDown,
    setEnvironment,
    startDeployment,
    cancelDeployment,
    skipToNextRegion,
    reset,
  } = useSmartDeploymentStore();

  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [prerequisitesReady, setPrerequisitesReady] = useState(false);
  const [discoveredResources, setDiscoveredResources] = useState<DiscoveredResources | null>(null);
  const [selectedResourceGroup, setSelectedResourceGroup] = useState<ResourceGroup | null>(null);
  const [selectedStaticWebApp, setSelectedStaticWebApp] = useState<StaticWebApp | null>(null);
  const [deployMode, setDeployMode] = useState<DeployMode>('auto');
  const [showAdvancedConfig, setShowAdvancedConfig] = useState(false);

  // Determine if infrastructure exists
  const hasInfrastructure = Boolean(
    (selectedResourceGroup && selectedResourceGroup.hasResources) ||
    selectedStaticWebApp ||
    (discoveredResources && (discoveredResources.resourceGroups.some(rg => rg.hasResources) || discoveredResources.staticWebApps.length > 0))
  );

  // Auto-select deploy mode based on infrastructure state
  useEffect(() => {
    if (deployMode === 'auto') {
      // Don't auto-switch - let user decide
    }
  }, [hasInfrastructure]);

  const handleCopy = async (value: string, field: string) => {
    await navigator.clipboard.writeText(value);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const handleResourcesDiscovered = (resources: DiscoveredResources) => {
    setDiscoveredResources(resources);
    // Auto-select the first resource group with resources if none selected
    if (!selectedResourceGroup && resources.resourceGroups.length > 0) {
      const rgWithResources = resources.resourceGroups.find(rg => rg.hasResources);
      if (rgWithResources) {
        setSelectedResourceGroup(rgWithResources);
      }
    }
  };

  const currentRegion = regionPriority[currentRegionIndex];
  const currentStatus = regionStatuses.get(currentRegion);

  // Determine the current resource group and region for config panel
  const currentResourceGroup = selectedResourceGroup?.name || `mys-dev-mystira-rg-euw`;
  const currentRegionCode = selectedResourceGroup?.location || 'southafricanorth';

  return (
    <div className="flex flex-col h-full bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="flex-shrink-0 p-4 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white flex items-center gap-2">
              <Rocket className="w-5 h-5 text-blue-500" />
              Deploy Now
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Smart deployment with auto-detection and fallback
            </p>
          </div>

          {/* Environment selector */}
          <div className="flex items-center gap-3">
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value as 'dev' | 'staging' | 'prod')}
              disabled={isDeploying}
              className="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-sm disabled:opacity-50"
            >
              <option value="dev">Development</option>
              <option value="staging">Staging</option>
              <option value="prod">Production</option>
            </select>
            <button
              onClick={() => setShowAdvancedConfig(!showAdvancedConfig)}
              className={`p-2 rounded-lg border transition-colors ${
                showAdvancedConfig
                  ? 'bg-blue-50 dark:bg-blue-900/30 border-blue-300 dark:border-blue-700 text-blue-600'
                  : 'border-gray-300 dark:border-gray-600 text-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700'
              }`}
              title="Advanced Configuration"
            >
              <Settings2 className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Main content - scrollable */}
      <div className="flex-1 overflow-auto p-4 space-y-4">
        {/* Prerequisites Check */}
        <PrerequisitesCheckPanel
          onAllReady={setPrerequisitesReady}
          onAzureAccountChange={setAzureAccount}
        />

        {/* Resource Discovery */}
        {prerequisitesReady && (
          <ResourceDiscoveryPanel
            onResourcesDiscovered={handleResourcesDiscovered}
            onResourceGroupSelected={setSelectedResourceGroup}
            onStaticWebAppSelected={setSelectedStaticWebApp}
            selectedResourceGroup={selectedResourceGroup}
          />
        )}

        {/* Smart Deploy Decision */}
        {prerequisitesReady && discoveredResources && (
          <SmartDeployDecision
            hasInfrastructure={hasInfrastructure}
            selectedResourceGroup={selectedResourceGroup}
            selectedStaticWebApp={selectedStaticWebApp}
            onDeployModeSelect={setDeployMode}
            selectedMode={deployMode}
          />
        )}

        {/* Mode-specific panels */}
        {prerequisitesReady && deployMode === 'infrastructure' && (
          <>
            {/* Region Priority for Infrastructure Deployment */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
              <div className="px-4 py-3 bg-gray-50 dark:bg-gray-700/50 flex items-center gap-2">
                <Globe className="w-4 h-4 text-blue-500" />
                <span className="font-medium text-gray-900 dark:text-white text-sm">
                  Region Priority
                </span>
                <span className="text-xs text-gray-500">(auto-fallback on failure)</span>
              </div>
              <div className="p-4 space-y-2">
                {regionPriority.map((regionId, index) => {
                  const region = AZURE_REGIONS.find(r => r.id === regionId)!;
                  const status = regionStatuses.get(regionId);

                  return (
                    <RegionRow
                      key={regionId}
                      region={region}
                      index={index}
                      status={status}
                      isCurrentTarget={isDeploying && index === currentRegionIndex}
                      canMoveUp={index > 0 && !isDeploying}
                      canMoveDown={index < regionPriority.length - 1 && !isDeploying}
                      onMoveUp={() => moveRegionUp(regionId)}
                      onMoveDown={() => moveRegionDown(regionId)}
                    />
                  );
                })}
              </div>
            </div>

            {/* Deployment Progress */}
            {isDeploying && currentStatus && (
              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-3">
                    <Loader2 className="w-5 h-5 text-blue-600 dark:text-blue-400 animate-spin" />
                    <span className="font-medium text-blue-900 dark:text-blue-200">
                      Deploying to {AZURE_REGIONS.find(r => r.id === currentRegion)?.name}...
                    </span>
                  </div>

                  <div className="flex gap-2">
                    <button
                      onClick={skipToNextRegion}
                      disabled={currentRegionIndex >= regionPriority.length - 1}
                      className="px-3 py-1 text-sm bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 rounded-md hover:bg-yellow-200 dark:hover:bg-yellow-900/50 disabled:opacity-50 flex items-center gap-1"
                    >
                      <SkipForward className="w-4 h-4" />
                      Skip
                    </button>
                    <button
                      onClick={cancelDeployment}
                      className="px-3 py-1 text-sm bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 rounded-md hover:bg-red-200 dark:hover:bg-red-900/50 flex items-center gap-1"
                    >
                      <Square className="w-4 h-4" />
                      Cancel
                    </button>
                  </div>
                </div>

                <div className="text-sm text-blue-700 dark:text-blue-300">
                  Attempt {currentRegionIndex + 1} of {regionPriority.length}
                </div>
              </div>
            )}

            {/* Success Result */}
            {deploymentResult?.success && (
              <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
                <div className="flex items-center gap-2 mb-3">
                  <CheckCircle2 className="w-5 h-5 text-green-600 dark:text-green-400" />
                  <span className="font-medium text-green-900 dark:text-green-200">
                    Infrastructure Deployment Successful!
                  </span>
                </div>

                <div className="space-y-2 text-sm">
                  <div className="flex justify-between items-center py-1 border-b border-green-200 dark:border-green-800">
                    <span className="text-green-700 dark:text-green-300">Region:</span>
                    <span className="font-mono text-green-900 dark:text-green-200">
                      {AZURE_REGIONS.find(r => r.id === deploymentResult.region)?.name}
                    </span>
                  </div>
                  <div className="flex justify-between items-center py-1 border-b border-green-200 dark:border-green-800">
                    <span className="text-green-700 dark:text-green-300">Resource Group:</span>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-green-900 dark:text-green-200">
                        {deploymentResult.resourceGroup}
                      </span>
                      <button onClick={() => handleCopy(deploymentResult.resourceGroup, 'rg')} className="p-1 hover:bg-green-200 dark:hover:bg-green-800 rounded">
                        {copiedField === 'rg' ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
                      </button>
                    </div>
                  </div>
                  {deploymentResult.apiUrl && (
                    <div className="flex justify-between items-center py-1">
                      <span className="text-green-700 dark:text-green-300">API URL:</span>
                      <div className="flex items-center gap-2">
                        <a href={deploymentResult.apiUrl} target="_blank" rel="noopener noreferrer" className="font-mono text-green-900 dark:text-green-200 hover:underline">
                          {deploymentResult.apiUrl}
                        </a>
                        <button onClick={() => handleCopy(deploymentResult.apiUrl!, 'api')} className="p-1 hover:bg-green-200 dark:hover:bg-green-800 rounded">
                          {copiedField === 'api' ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
                        </button>
                      </div>
                    </div>
                  )}
                </div>

                {/* Next step suggestion */}
                <div className="mt-4 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                  <p className="text-sm text-blue-700 dark:text-blue-300">
                    <strong>Next step:</strong> Switch to "Deploy Code" mode to push your application code.
                  </p>
                </div>
              </div>
            )}

            {/* All Failed */}
            {allAttemptsFailed && (
              <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                <div className="flex items-center gap-2 mb-3">
                  <XCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
                  <span className="font-medium text-red-900 dark:text-red-200">
                    Deployment Failed
                  </span>
                </div>
                <p className="text-sm text-red-700 dark:text-red-300 mb-3">
                  All regions have been exhausted. Check the logs below for details.
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={reset}
                    className="px-3 py-1 text-sm bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 rounded-md hover:bg-red-200 flex items-center gap-1"
                  >
                    <RefreshCw className="w-4 h-4" />
                    Reset & Try Again
                  </button>
                </div>
              </div>
            )}
          </>
        )}

        {/* Code Deployment Mode */}
        {prerequisitesReady && deployMode === 'code' && (
          <GitOperationsPanel
            repoRoot={repoRoot}
            onDeployTriggered={() => {
              // Could trigger a resource refresh here
            }}
            disabled={!hasInfrastructure}
          />
        )}

        {/* Configuration Panel */}
        {prerequisitesReady && showAdvancedConfig && (
          <ConfigurationPanel
            resourceGroup={currentResourceGroup}
            region={currentRegionCode}
            disabled={isDeploying}
          />
        )}

        {/* Logs */}
        {(logs.length > 0 || isDeploying) && (
          <div className="bg-gray-900 rounded-lg overflow-hidden">
            <div className="px-4 py-2 bg-gray-800 border-b border-gray-700 flex items-center justify-between">
              <span className="text-sm font-medium text-gray-300">Deployment Logs</span>
              <span className="text-xs text-gray-500">{logs.length} entries</span>
            </div>
            <div className="p-4 h-48 overflow-auto font-mono text-xs text-gray-300 space-y-1">
              {logs.length === 0 ? (
                <span className="text-gray-500">Waiting for deployment to start...</span>
              ) : (
                logs.map((log, i) => (
                  <div key={i} className={getLogClass(log)}>
                    {log}
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </div>

      {/* Footer with action buttons */}
      <div className="flex-shrink-0 p-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        <div className="flex justify-between items-center">
          <div className="text-sm text-gray-500 dark:text-gray-400">
            {!prerequisitesReady ? (
              <span className="flex items-center gap-2 text-yellow-600 dark:text-yellow-400">
                <AlertTriangle className="w-4 h-4" />
                Complete prerequisites setup above
              </span>
            ) : isDeploying ? (
              <span className="flex items-center gap-2">
                <Loader2 className="w-4 h-4 animate-spin" />
                Deployment in progress...
              </span>
            ) : deploymentResult?.success ? (
              <span className="flex items-center gap-2 text-green-600 dark:text-green-400">
                <CheckCircle2 className="w-4 h-4" />
                Deployed to {AZURE_REGIONS.find(r => r.id === deploymentResult.region)?.name}
              </span>
            ) : deployMode === 'code' ? (
              'Ready to deploy code via git push'
            ) : (
              'Ready to deploy infrastructure'
            )}
          </div>

          <div className="flex gap-2">
            {deploymentResult?.success && (
              <button
                onClick={reset}
                className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600"
              >
                Reset
              </button>
            )}

            {deployMode === 'infrastructure' && (
              <button
                onClick={() => startDeployment(repoRoot)}
                disabled={isDeploying || !prerequisitesReady}
                className="px-4 py-2 text-sm font-medium text-white bg-purple-600 rounded-lg hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                {isDeploying ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Deploying...
                  </>
                ) : (
                  <>
                    <Server className="w-4 h-4" />
                    Deploy Infrastructure
                  </>
                )}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

// Region row component
interface RegionRowProps {
  region: typeof AZURE_REGIONS[number];
  index: number;
  status?: RegionStatus;
  isCurrentTarget: boolean;
  canMoveUp: boolean;
  canMoveDown: boolean;
  onMoveUp: () => void;
  onMoveDown: () => void;
}

function RegionRow({
  region,
  index,
  status,
  isCurrentTarget,
  canMoveUp,
  canMoveDown,
  onMoveUp,
  onMoveDown,
}: RegionRowProps) {
  const getStatusIcon = () => {
    switch (status?.status) {
      case 'deploying':
        return <Loader2 className="w-4 h-4 text-blue-500 animate-spin" />;
      case 'success':
        return <CheckCircle2 className="w-4 h-4 text-green-500" />;
      case 'failed':
        return <XCircle className="w-4 h-4 text-red-500" />;
      case 'skipped':
        return <Clock className="w-4 h-4 text-gray-400" />;
      default:
        return <div className="w-4 h-4 rounded-full border-2 border-gray-300 dark:border-gray-600" />;
    }
  };

  const getRowClass = () => {
    let base = 'flex items-center justify-between p-3 rounded-lg border transition-colors';

    if (isCurrentTarget) {
      return `${base} bg-blue-50 dark:bg-blue-900/30 border-blue-300 dark:border-blue-700`;
    }
    if (status?.status === 'success') {
      return `${base} bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800`;
    }
    if (status?.status === 'failed') {
      return `${base} bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800`;
    }
    if (status?.status === 'skipped') {
      return `${base} bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700 opacity-50`;
    }

    return `${base} bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700`;
  };

  return (
    <div className={getRowClass()}>
      <div className="flex items-center gap-3">
        <span className="w-6 h-6 flex items-center justify-center text-xs font-bold text-gray-400 bg-gray-100 dark:bg-gray-700 rounded">
          {index + 1}
        </span>
        {getStatusIcon()}
        <div>
          <div className="font-medium text-gray-900 dark:text-white text-sm">
            {region.name}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400">
            {region.id} ({region.code})
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2">
        {status?.error && (
          <div className="text-xs text-red-600 dark:text-red-400 max-w-48 truncate" title={status.error}>
            {status.isRetryable ? (
              <span className="flex items-center gap-1">
                <AlertTriangle className="w-3 h-3" />
                Region issue
              </span>
            ) : (
              status.error.slice(0, 30) + '...'
            )}
          </div>
        )}

        <div className="flex flex-col">
          <button
            onClick={onMoveUp}
            disabled={!canMoveUp}
            className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 disabled:opacity-30 disabled:cursor-not-allowed"
          >
            <ChevronUp className="w-4 h-4" />
          </button>
          <button
            onClick={onMoveDown}
            disabled={!canMoveDown}
            className="p-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 disabled:opacity-30 disabled:cursor-not-allowed"
          >
            <ChevronDown className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  );
}

function getLogClass(log: string): string {
  if (log.includes('✓') || log.includes('successful') || log.includes('Success')) {
    return 'text-green-400';
  }
  if (log.includes('✗') || log.includes('failed') || log.includes('Failed') || log.includes('Error')) {
    return 'text-red-400';
  }
  if (log.includes('Attempting') || log.includes('Trying') || log.includes('...')) {
    return 'text-blue-400';
  }
  if (log.includes('Skip') || log.includes('Cancel')) {
    return 'text-yellow-400';
  }
  return 'text-gray-300';
}

export default SmartDeploymentPanel;
