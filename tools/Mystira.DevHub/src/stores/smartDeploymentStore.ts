import { invoke } from '@tauri-apps/api/tauri';
import { create } from 'zustand';
import type { CommandResponse } from '../types';

// Regions that support all Azure services we need (Cosmos, App Service, Storage, Static Web Apps)
export const AZURE_REGIONS = [
  { id: 'eastus2', name: 'East US 2', code: 'eus2' },
  { id: 'westus2', name: 'West US 2', code: 'wus2' },
  { id: 'centralus', name: 'Central US', code: 'cus' },
  { id: 'westeurope', name: 'West Europe', code: 'euw' },
  { id: 'northeurope', name: 'North Europe', code: 'eun' },
  { id: 'eastasia', name: 'East Asia', code: 'ea' },
  { id: 'southeastasia', name: 'Southeast Asia', code: 'sea' },
] as const;

export type RegionId = typeof AZURE_REGIONS[number]['id'];

export interface RegionStatus {
  regionId: RegionId;
  status: 'pending' | 'deploying' | 'success' | 'failed' | 'skipped';
  message?: string;
  startTime?: Date;
  endTime?: Date;
  error?: string;
  isRetryable?: boolean; // True if it's a region/capacity issue, false if config error
}

export interface DeploymentResult {
  success: boolean;
  region: RegionId;
  resourceGroup: string;
  apiUrl?: string;
  adminApiUrl?: string;
  jwtSecret?: string;
  logs?: string;
}

interface SmartDeploymentState {
  // Configuration
  regionPriority: RegionId[];
  environment: 'dev' | 'staging' | 'prod';

  // Deployment state
  isDeploying: boolean;
  isCancelled: boolean;
  currentRegionIndex: number;
  regionStatuses: Map<RegionId, RegionStatus>;

  // Results
  deploymentResult: DeploymentResult | null;
  allAttemptsFailed: boolean;

  // Logs
  logs: string[];

  // Actions
  setRegionPriority: (regions: RegionId[]) => void;
  moveRegionUp: (regionId: RegionId) => void;
  moveRegionDown: (regionId: RegionId) => void;
  setEnvironment: (env: 'dev' | 'staging' | 'prod') => void;

  startDeployment: (repoRoot: string) => Promise<void>;
  cancelDeployment: () => void;
  skipToNextRegion: () => void;
  retryCurrentRegion: () => void;

  addLog: (message: string) => void;
  clearLogs: () => void;
  reset: () => void;
}

function getResourceGroupName(env: string, regionCode: string): string {
  return `${env}-${regionCode}-rg-mystira-app`;
}

export const useSmartDeploymentStore = create<SmartDeploymentState>((set, get) => ({
  // Default configuration
  regionPriority: ['eastus2', 'westus2', 'centralus', 'westeurope', 'northeurope', 'eastasia'],
  environment: 'dev',

  // Initial state
  isDeploying: false,
  isCancelled: false,
  currentRegionIndex: 0,
  regionStatuses: new Map(),
  deploymentResult: null,
  allAttemptsFailed: false,
  logs: [],

  // Region priority management
  setRegionPriority: (regions) => set({ regionPriority: regions }),

  moveRegionUp: (regionId) => {
    const { regionPriority, isDeploying } = get();
    if (isDeploying) return; // Don't allow changes during deployment

    const index = regionPriority.indexOf(regionId);
    if (index > 0) {
      const newPriority = [...regionPriority];
      [newPriority[index - 1], newPriority[index]] = [newPriority[index], newPriority[index - 1]];
      set({ regionPriority: newPriority });
    }
  },

  moveRegionDown: (regionId) => {
    const { regionPriority, isDeploying } = get();
    if (isDeploying) return;

    const index = regionPriority.indexOf(regionId);
    if (index < regionPriority.length - 1) {
      const newPriority = [...regionPriority];
      [newPriority[index], newPriority[index + 1]] = [newPriority[index + 1], newPriority[index]];
      set({ regionPriority: newPriority });
    }
  },

  setEnvironment: (env) => set({ environment: env }),

  addLog: (message) => {
    const timestamp = new Date().toLocaleTimeString();
    set(state => ({ logs: [...state.logs, `[${timestamp}] ${message}`] }));
  },

  clearLogs: () => set({ logs: [] }),

  reset: () => set({
    isDeploying: false,
    isCancelled: false,
    currentRegionIndex: 0,
    regionStatuses: new Map(),
    deploymentResult: null,
    allAttemptsFailed: false,
    logs: [],
  }),

  // Main deployment function with fallback
  startDeployment: async (repoRoot: string) => {
    const { regionPriority, environment, addLog } = get();

    // Reset state
    set({
      isDeploying: true,
      isCancelled: false,
      currentRegionIndex: 0,
      regionStatuses: new Map(regionPriority.map(r => [r, { regionId: r, status: 'pending' as const }])),
      deploymentResult: null,
      allAttemptsFailed: false,
      logs: [],
    });

    addLog(`Starting smart deployment for ${environment} environment`);
    addLog(`Region priority: ${regionPriority.join(' → ')}`);

    for (let i = 0; i < regionPriority.length; i++) {
      const { isCancelled } = get();
      if (isCancelled) {
        addLog('Deployment cancelled by user');
        break;
      }

      const regionId = regionPriority[i];
      const region = AZURE_REGIONS.find(r => r.id === regionId)!;
      const resourceGroup = getResourceGroupName(environment, region.code);

      set({ currentRegionIndex: i });

      // Update status to deploying
      set(state => {
        const newStatuses = new Map(state.regionStatuses);
        newStatuses.set(regionId, {
          regionId,
          status: 'deploying',
          startTime: new Date(),
        });
        return { regionStatuses: newStatuses };
      });

      addLog(`Attempting deployment to ${region.name} (${regionId})...`);
      addLog(`Resource group: ${resourceGroup}`);

      try {
        // First, create resource group
        addLog(`Creating resource group ${resourceGroup}...`);
        const rgResponse = await invoke<CommandResponse>('azure_create_resource_group', {
          resourceGroup,
          location: regionId,
        });

        if (!rgResponse.success && !rgResponse.message?.includes('already exists')) {
          throw new Error(rgResponse.error || 'Failed to create resource group');
        }

        // Check for cancellation again
        if (get().isCancelled) {
          addLog('Deployment cancelled during resource group creation');
          break;
        }

        // Deploy infrastructure
        addLog(`Deploying infrastructure to ${region.name}...`);
        const deployResponse = await invoke<CommandResponse>('azure_deploy_infrastructure', {
          repoRoot,
          environment,
          resourceGroup,
          location: regionId,
          deployStorage: true,
          deployCosmos: true,
          deployAppService: true,
        });

        if (deployResponse.success) {
          // Success!
          const result = deployResponse.result as Record<string, unknown>;

          set(state => {
            const newStatuses = new Map(state.regionStatuses);
            newStatuses.set(regionId, {
              regionId,
              status: 'success',
              startTime: state.regionStatuses.get(regionId)?.startTime,
              endTime: new Date(),
              message: 'Deployment successful',
            });
            return { regionStatuses: newStatuses };
          });

          const deploymentResult: DeploymentResult = {
            success: true,
            region: regionId,
            resourceGroup,
            apiUrl: (result.outputs as Record<string, { value: string }>)?.apiAppServiceUrl?.value,
            adminApiUrl: (result.outputs as Record<string, { value: string }>)?.adminApiAppServiceUrl?.value,
            logs: result.logs as string,
          };

          addLog(`✓ Deployment successful in ${region.name}!`);
          addLog(`Resource Group: ${resourceGroup}`);
          if (deploymentResult.apiUrl) addLog(`API URL: ${deploymentResult.apiUrl}`);

          set({
            isDeploying: false,
            deploymentResult,
          });

          // Mark remaining regions as skipped
          const remaining = regionPriority.slice(i + 1);
          set(state => {
            const newStatuses = new Map(state.regionStatuses);
            remaining.forEach(r => {
              newStatuses.set(r, { regionId: r, status: 'skipped', message: 'Not needed - deployment succeeded' });
            });
            return { regionStatuses: newStatuses };
          });

          return; // Exit on success
        } else {
          // Deployment failed - check if it's a retryable error
          const error = deployResponse.error || 'Unknown error';
          const isRetryable = isRegionError(error);

          set(state => {
            const newStatuses = new Map(state.regionStatuses);
            newStatuses.set(regionId, {
              regionId,
              status: 'failed',
              startTime: state.regionStatuses.get(regionId)?.startTime,
              endTime: new Date(),
              error,
              isRetryable,
            });
            return { regionStatuses: newStatuses };
          });

          addLog(`✗ Failed in ${region.name}: ${error}`);

          if (isRetryable) {
            addLog(`Region issue detected - trying next fallback...`);
          } else {
            addLog(`Non-recoverable error - stopping deployment`);
            set({ isDeploying: false, allAttemptsFailed: true });
            return;
          }
        }
      } catch (error) {
        const errorMsg = error instanceof Error ? error.message : String(error);
        const isRetryable = isRegionError(errorMsg);

        set(state => {
          const newStatuses = new Map(state.regionStatuses);
          newStatuses.set(regionId, {
            regionId,
            status: 'failed',
            startTime: state.regionStatuses.get(regionId)?.startTime,
            endTime: new Date(),
            error: errorMsg,
            isRetryable,
          });
          return { regionStatuses: newStatuses };
        });

        addLog(`✗ Error in ${region.name}: ${errorMsg}`);

        if (!isRetryable) {
          set({ isDeploying: false, allAttemptsFailed: true });
          return;
        }
      }
    }

    // If we get here, all regions failed
    const { isCancelled } = get();
    if (!isCancelled) {
      addLog('All regions exhausted - deployment failed');
      set({ isDeploying: false, allAttemptsFailed: true });
    } else {
      set({ isDeploying: false });
    }
  },

  cancelDeployment: () => {
    const { addLog } = get();
    addLog('Cancellation requested...');
    set({ isCancelled: true });
  },

  skipToNextRegion: () => {
    const { currentRegionIndex, regionPriority, addLog } = get();
    if (currentRegionIndex < regionPriority.length - 1) {
      const skippedRegion = regionPriority[currentRegionIndex];
      addLog(`Skipping ${skippedRegion}, moving to next region...`);

      set(state => {
        const newStatuses = new Map(state.regionStatuses);
        newStatuses.set(skippedRegion, {
          regionId: skippedRegion,
          status: 'skipped',
          message: 'Skipped by user',
        });
        return { regionStatuses: newStatuses };
      });
    }
  },

  retryCurrentRegion: () => {
    const { addLog } = get();
    addLog('Retry requested for current region...');
    // The deployment loop will handle this naturally
  },
}));

// Helper to determine if an error is a region/capacity issue (retryable)
function isRegionError(error: string): boolean {
  const retryablePatterns = [
    'LocationNotAvailable',
    'ServiceUnavailable',
    'capacity',
    'quota',
    'not available',
    'region',
    'location',
    'RequestDisallowedByPolicy',
    'SkuNotAvailable',
  ];

  const lowerError = error.toLowerCase();
  return retryablePatterns.some(pattern => lowerError.includes(pattern.toLowerCase()));
}
